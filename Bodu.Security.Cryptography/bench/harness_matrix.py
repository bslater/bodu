#!/usr/bin/env python3
"""Runs the cryptography harnesses in a matrix of builds and runtimes on the machine at hand, and summarizes the runs.

    python3 harness_matrix.py run --out results [--runs 2] [--suites crypto,argon2,sweep] [--frameworks net10.0,net8.0]
    python3 harness_matrix.py summarize results

``run`` builds the benchmark project for each framework twice: from this checkout, and against the published 1.0.0
package (``-p:BoduCryptoBaseline=1.0.0``). It then runs each suite in its configurations, reversing their order from
one run to the next so that drift over the job falls on each of them alike:

* ``vector``: this checkout, with the kernels dispatch selects;
* ``avx2``: this checkout with the runtime's AVX-512 switched off (``DOTNET_EnableAVX512F=0`` for .NET 8,
  ``DOTNET_EnableAVX512=0`` for .NET 10), so that on x64 the AVX2 kernels run where the AVX-512 ones would; the ``x64``
  suite alone runs it, and on a processor without AVX-512 it repeats ``vector``;
* ``ssse3``: this checkout with the runtime's AVX2 switched off (``DOTNET_EnableAVX2=0``), so that on x64 the SSSE3
  kernels run where the AVX2 ones would; the ``x64`` suite alone runs it;
* ``scalar``: this checkout with the library's ``DisableSimd`` switch set (``--disable-simd``), so every kernel gives
  way to its scalar path while the runtime and the BCL keep their vector code;
* ``1.0.0``: the published package.

The suites are ``crypto`` (``--crypto-harness`` over the cases that exercise the AdvSimd, PMULL and ``umulh`` paths),
``x64`` (``--crypto-harness`` over the cases whose x64 kernels spread one state across a vector's lanes: BLAKE2,
BLAKE3's single block, scrypt and Argon2), ``argon2`` (``--argon2-harness``) and ``sweep``
(``--argon2-harness --sweep``). Each run of each configuration writes
``<out>/<suite>/<run>-<framework>-<configuration>.txt``.

``summarize`` prints a Markdown table per suite and framework: each case's range over the runs in every configuration,
and how many times faster the vector configuration is than the others, by the medians.
"""

import argparse
import os
import re
import statistics
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.join(HERE, 'Bodu.Security.Cryptography.Benchmarks.csproj')
ASSEMBLY = 'Bodu.Security.Cryptography.Benchmarks.dll'
CONFIGURATIONS = ['vector', 'avx2', 'ssse3', 'scalar', '1.0.0']

# Each configuration: the build it runs, the harness arguments it adds, and the environment it sets.
CONFIGURATION_SETTINGS = {
    'vector': ('current', [], {}),
    'avx2': ('current', [], {'DOTNET_EnableAVX512F': '0', 'DOTNET_EnableAVX512': '0'}),
    'ssse3': ('current', [], {'DOTNET_EnableAVX2': '0'}),
    'scalar': ('current', ['--disable-simd'], {}),
    '1.0.0': ('1.0.0', [], {}),
}

# The crypto-harness cases whose code has an ARM64 path of its own: the AdvSimd kernels (BLAKE2b, BLAKE2s, BLAKE3,
# CubeHash, ChaCha20 and Salsa20, Poly1305, Serpent-128, scrypt, Argon2), the PMULL GHASH and POLYVAL kernels (GCM,
# GCM-SIV), and the umulh products of Poly1305's scalar loop and of Curve25519, which the library's switch leaves in
# place. Poly1305 and its AEADs run at lengths either side of the AdvSimd kernel's thresholds, and the kernel cases time
# each BLAKE, Poly1305 and Argon2 kernel on its own, the BLAKE kernels that dispatch holds back on ARM64 included.
CRYPTO_FILTERS = [
    'hash/Bodu BLAKE2', 'hash/Bodu BLAKE3', 'kernel/BLAKE', 'hash/Bodu CubeHash',
    'stream/Bodu ChaCha20', 'stream/Bodu XChaCha20', 'stream/Bodu Salsa20', 'stream/Bodu XSalsa20',
    'Poly1305 64 B', 'Poly1305 128 B', 'Poly1305 192 B', 'Poly1305 256 B', 'Poly1305 512 B', 'Poly1305 1 KiB',
    'Poly1305 1 MiB', 'kernel/Poly1305', 'kernel/Argon2', 'GCM',
    'Serpent-128-', 'kdf/', 'asym/Bodu X25519', 'asym/Bodu Ed25519 sign', 'asym/Bodu Ed25519 verify',
]

# The crypto-harness cases whose x64 kernels spread one state across a vector's lanes, the layout F7 found losing on
# ARM64: BLAKE2b, BLAKE2s, BLAKE3's single block, scrypt and Argon2. The ``avx2`` configuration runs their AVX2 kernels
# whether or not the machine has AVX-512, and ``ssse3`` their 128-bit ones, and the kernel cases time each BLAKE kernel
# the configuration allows on its own.
X64_FILTERS = ['hash/Bodu BLAKE2', 'hash/Bodu BLAKE3', 'kernel/BLAKE', 'kdf/Bodu']

# Each suite: its harness arguments, and the configurations it runs in.
SUITES = {
    'crypto': (['--crypto-harness', *CRYPTO_FILTERS], ['vector', 'scalar', '1.0.0']),
    'x64': (['--crypto-harness', *X64_FILTERS], ['vector', 'avx2', 'ssse3', 'scalar', '1.0.0']),
    'argon2': (['--argon2-harness'], ['vector', 'scalar', '1.0.0']),
    'sweep': (['--argon2-harness', '--sweep'], ['vector', 'scalar', '1.0.0']),
}

CRYPTO_LINE = re.compile(r'^(\w+)\s+(.+?)\s{2,}([\d.,]+) (us|ms)\s+([\d.,]+) (MiB/s|op/s)\s+[\d,]+ B/op')
ARGON2_LINE = re.compile(r'^(.+?)\s+wall\s+([\d.]+) ms\s+cpu\s+([\d.]+) ms')
ARGON2_CONCURRENT_LINE = re.compile(r'^(.+?)\s+\d+ in\s+\d+ ms\s+([\d.]+) ms each\s+cpu\s+([\d.]+) ms each')


def build(out, frameworks):
    """Builds the benchmarks from this checkout and against 1.0.0, for each framework."""
    for configuration, properties in (('current', []), ('1.0.0', ['-p:BoduCryptoBaseline=1.0.0'])):
        for framework in frameworks:
            output = os.path.join(out, 'bin', configuration, framework)
            command = ['dotnet', 'build', PROJECT, '-c', 'Release', '-f', framework, '-o', output, *properties]
            print(f'::group::build {configuration} {framework}', flush=True)
            subprocess.run(command, check=True)
            print('::endgroup::', flush=True)


def run(out, runs, suites, frameworks):
    """Runs every suite in every configuration, reversing the configurations' order on alternate runs."""
    for run_number in range(1, runs + 1):
        order = CONFIGURATIONS if run_number % 2 else list(reversed(CONFIGURATIONS))
        for framework in frameworks:
            for configuration in order:
                binary, extra, environment = CONFIGURATION_SETTINGS[configuration]
                for suite in suites:
                    suite_arguments, configurations = SUITES[suite]
                    if configuration not in configurations:
                        continue

                    arguments = [*extra, *suite_arguments]
                    directory = os.path.join(out, suite)
                    os.makedirs(directory, exist_ok=True)
                    path = os.path.join(directory, f'{run_number}-{framework}-{configuration}.txt')
                    assembly = os.path.join(out, 'bin', binary, framework, ASSEMBLY)
                    print(f'::group::run {run_number} {framework} {configuration} {suite}', flush=True)
                    result = subprocess.run(
                        ['dotnet', assembly, *arguments],
                        capture_output=True,
                        text=True,
                        check=False,
                        env={**os.environ, **environment})
                    with open(path, 'w', encoding='utf-8') as file:
                        file.write(result.stdout)
                        file.write(result.stderr)

                    sys.stdout.write(result.stdout + result.stderr)
                    print('::endgroup::', flush=True)
                    if result.returncode != 0:
                        raise SystemExit(f'{assembly} {" ".join(arguments)} exited with {result.returncode}')


def parse(path):
    """Returns the header line and a map of case to measured values from one harness output."""
    header, values = '', {}
    with open(path, encoding='utf-8') as file:
        for line in file:
            line = line.rstrip('\n')
            if line.startswith('Bodu.Security.Cryptography '):
                header = line
            elif match := CRYPTO_LINE.match(line):
                # A throughput case reads its rate; a per-operation case its time, since the harness prints its rate in
                # whole operations a second, too coarse for the slow ones.
                case = f'{match.group(1)}/{match.group(2).strip()}'
                if match.group(6) == 'MiB/s':
                    values[case] = (float(match.group(5).replace(',', '')), 'MiB/s')
                else:
                    time = float(match.group(3).replace(',', ''))
                    values[case] = (time * 1000 if match.group(4) == 'ms' else time, 'µs/op')
            elif match := ARGON2_LINE.match(line):
                values[f'{match.group(1).strip()}: wall'] = (float(match.group(2)), 'ms')
                values[f'{match.group(1).strip()}: cpu'] = (float(match.group(3)), 'ms')
            elif match := ARGON2_CONCURRENT_LINE.match(line):
                values[f'{match.group(1).strip()}: wall each'] = (float(match.group(2)), 'ms')
                values[f'{match.group(1).strip()}: cpu each'] = (float(match.group(3)), 'ms')

    return header, values


def span(values):
    """Formats the range of a list of measurements."""
    low, high = min(values), max(values)
    digits = 0 if high >= 100 else 1 if high >= 10 else 2
    text = f'{low:,.{digits}f}'
    return text if low == high else f'{text}-{high:,.{digits}f}'


def speedup(vector, other, unit):
    """Returns how many times faster the first configuration is, by the medians: rates divide one way, times the other.

    A median of zero, as a derivation shorter than the processor clock's resolution records for its processor time,
    gives no ratio.
    """
    a, b = statistics.median(vector), statistics.median(other)
    if a == 0 or b == 0:
        return '-'
    ratio = a / b if unit == 'MiB/s' else b / a
    return f'{ratio:.2f}×'


def summarize(out):
    """Prints a Markdown table per suite and framework."""
    for suite in sorted(os.listdir(out)):
        directory = os.path.join(out, suite)
        if suite == 'bin' or not os.path.isdir(directory):
            continue

        files = sorted(os.listdir(directory))
        frameworks = sorted({name.split('-')[1] for name in files}, reverse=True)
        for framework in frameworks:
            measured, headers, units = {}, {}, {}
            for name in files:
                run_number, file_framework, configuration = name[:-4].split('-', 2)
                if file_framework != framework:
                    continue

                header, values = parse(os.path.join(directory, name))
                headers.setdefault(configuration, header)
                for case, (value, unit) in values.items():
                    measured.setdefault(case, {}).setdefault(configuration, []).append(value)
                    units[case] = unit

            print(f'### {suite}, {framework}\n')
            for configuration in CONFIGURATIONS:
                if configuration in headers:
                    print(f'- `{configuration}`: {headers[configuration]}')

            print()
            present = [c for c in CONFIGURATIONS if c in headers]
            comparisons = [(a, b) for a, b in (('vector', 'scalar'), ('avx2', 'scalar'), ('ssse3', 'scalar'), ('vector', '1.0.0')) if a in present and b in present]
            print('| Case | Unit | ' + ' | '.join(present) + ' | ' + ' | '.join(f'{a} vs {b}' for a, b in comparisons) + ' |')
            print('|---|---|' + '---|' * (len(present) + len(comparisons)))
            for case, by_configuration in measured.items():
                cells = [span(by_configuration[c]) if c in by_configuration else '-' for c in present]
                versus = [
                    speedup(by_configuration[a], by_configuration[b], units[case])
                    if a in by_configuration and b in by_configuration else '-'
                    for a, b in comparisons
                ]
                print(f'| {case} | {units[case]} | ' + ' | '.join(cells + versus) + ' |')

            print()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest='command', required=True)
    run_parser = commands.add_parser('run', help='build, then run every suite in every configuration')
    run_parser.add_argument('--out', required=True)
    run_parser.add_argument('--runs', type=int, default=2)
    run_parser.add_argument('--suites', default='crypto,argon2,sweep')
    run_parser.add_argument('--frameworks', default='net10.0,net8.0')
    run_parser.add_argument('--skip-build', action='store_true')
    summarize_parser = commands.add_parser('summarize', help='print the runs as Markdown tables')
    summarize_parser.add_argument('out')
    arguments = parser.parse_args()

    if arguments.command == 'run':
        frameworks = arguments.frameworks.split(',')
        suites = arguments.suites.split(',')
        if not arguments.skip_build:
            build(arguments.out, frameworks)

        run(arguments.out, arguments.runs, suites, frameworks)
    else:
        summarize(arguments.out)


if __name__ == '__main__':
    main()
