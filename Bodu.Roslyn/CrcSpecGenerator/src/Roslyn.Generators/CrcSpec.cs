using System;
using System.Collections.Generic;
using System.Text;

namespace Bodu.Rosyln.Generators
{
    public sealed class CrcSpec
    {
        public string[] Aliases { get; set; }

        public string Check { get; set; }

        public string Class { get; set; }

        public string[] Codewords { get; set; }

        public string? Created { get; set; }

        public string InitialValue { get; set; }

        public string Name { get; set; }

        public string Polynomial { get; set; }

        public bool ReflectIn { get; set; }

        public bool ReflectOut { get; set; }

        public string Residue { get; set; }

        public int Size { get; set; }

        public string? Updated { get; set; }

        public string XorOut { get; set; }
    }
}