// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmKnownAnswers.Easter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Easter Sunday known-answer providers split into a partial of <see cref="NotableDateAlgorithmKnownAnswers" /> so the
/// main file stays scannable. Hosts the smoke, default-calendar full, and Orthodox (Julian-paschal) full tables, plus
/// the per-row helpers that wrap each <see cref="EasterSundayNotableDateAlgorithm" /> row.
/// </summary>
public static partial class NotableDateAlgorithmKnownAnswers
{
    /// <summary>
    /// Provides the smoke subset of Easter Sunday known-answer rows. Five representative years spanning the
    /// Julian/Gregorian transition era to the late 21st century, matched exactly against published almanac dates with
    /// the default (Gregorian) calendar.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> EasterSmoke()
    {
        yield return EasterRow(1700, new DateTime(1700, 4, 11));
        yield return EasterRow(1900, new DateTime(1900, 4, 15));
        yield return EasterRow(2000, new DateTime(2000, 4, 23));
        yield return EasterRow(2024, new DateTime(2024, 3, 31));
        yield return EasterRow(2030, new DateTime(2030, 4, 21));
    }

    /// <summary>
    /// Provides the full default-calendar Easter Sunday known-answer table — 376 rows spanning years 1700-2093, matched
    /// exactly against published almanac dates with the default (Gregorian) calendar.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> EasterDefault()
    {
        yield return EasterRow(1700, new DateTime(1700, 4, 11));
        yield return EasterRow(1701, new DateTime(1701, 3, 27));
        yield return EasterRow(1702, new DateTime(1702, 4, 16));
        yield return EasterRow(1703, new DateTime(1703, 4, 8));
        yield return EasterRow(1704, new DateTime(1704, 3, 23));
        yield return EasterRow(1705, new DateTime(1705, 4, 12));
        yield return EasterRow(1706, new DateTime(1706, 4, 4));
        yield return EasterRow(1707, new DateTime(1707, 4, 24));
        yield return EasterRow(1708, new DateTime(1708, 4, 8));
        yield return EasterRow(1709, new DateTime(1709, 3, 31));
        yield return EasterRow(1710, new DateTime(1710, 4, 20));
        yield return EasterRow(1711, new DateTime(1711, 4, 5));
        yield return EasterRow(1712, new DateTime(1712, 3, 27));
        yield return EasterRow(1713, new DateTime(1713, 4, 16));
        yield return EasterRow(1714, new DateTime(1714, 4, 1));
        yield return EasterRow(1715, new DateTime(1715, 4, 21));
        yield return EasterRow(1716, new DateTime(1716, 4, 12));
        yield return EasterRow(1717, new DateTime(1717, 3, 28));
        yield return EasterRow(1718, new DateTime(1718, 4, 17));
        yield return EasterRow(1719, new DateTime(1719, 4, 9));
        yield return EasterRow(1720, new DateTime(1720, 3, 31));
        yield return EasterRow(1721, new DateTime(1721, 4, 13));
        yield return EasterRow(1722, new DateTime(1722, 4, 5));
        yield return EasterRow(1723, new DateTime(1723, 3, 28));
        yield return EasterRow(1724, new DateTime(1724, 4, 16));
        yield return EasterRow(1725, new DateTime(1725, 4, 1));
        yield return EasterRow(1726, new DateTime(1726, 4, 21));
        yield return EasterRow(1727, new DateTime(1727, 4, 13));
        yield return EasterRow(1728, new DateTime(1728, 3, 28));
        yield return EasterRow(1729, new DateTime(1729, 4, 17));
        yield return EasterRow(1730, new DateTime(1730, 4, 9));
        yield return EasterRow(1731, new DateTime(1731, 3, 25));
        yield return EasterRow(1732, new DateTime(1732, 4, 13));
        yield return EasterRow(1733, new DateTime(1733, 4, 5));
        yield return EasterRow(1734, new DateTime(1734, 4, 25));
        yield return EasterRow(1735, new DateTime(1735, 4, 10));
        yield return EasterRow(1736, new DateTime(1736, 4, 1));
        yield return EasterRow(1737, new DateTime(1737, 4, 21));
        yield return EasterRow(1738, new DateTime(1738, 4, 6));
        yield return EasterRow(1739, new DateTime(1739, 3, 29));
        yield return EasterRow(1740, new DateTime(1740, 4, 17));
        yield return EasterRow(1741, new DateTime(1741, 4, 2));
        yield return EasterRow(1742, new DateTime(1742, 3, 25));
        yield return EasterRow(1743, new DateTime(1743, 4, 14));
        yield return EasterRow(1744, new DateTime(1744, 4, 5));
        yield return EasterRow(1745, new DateTime(1745, 4, 18));
        yield return EasterRow(1746, new DateTime(1746, 4, 10));
        yield return EasterRow(1747, new DateTime(1747, 4, 2));
        yield return EasterRow(1748, new DateTime(1748, 4, 14));
        yield return EasterRow(1749, new DateTime(1749, 4, 6));
        yield return EasterRow(1750, new DateTime(1750, 3, 29));
        yield return EasterRow(1751, new DateTime(1751, 4, 11));
        yield return EasterRow(1752, new DateTime(1752, 4, 2));
        yield return EasterRow(1753, new DateTime(1753, 4, 22));
        yield return EasterRow(1754, new DateTime(1754, 4, 14));
        yield return EasterRow(1755, new DateTime(1755, 3, 30));
        yield return EasterRow(1756, new DateTime(1756, 4, 18));
        yield return EasterRow(1757, new DateTime(1757, 4, 10));
        yield return EasterRow(1758, new DateTime(1758, 3, 26));
        yield return EasterRow(1759, new DateTime(1759, 4, 15));
        yield return EasterRow(1760, new DateTime(1760, 4, 6));
        yield return EasterRow(1761, new DateTime(1761, 3, 22));
        yield return EasterRow(1762, new DateTime(1762, 4, 11));
        yield return EasterRow(1763, new DateTime(1763, 4, 3));
        yield return EasterRow(1764, new DateTime(1764, 4, 22));
        yield return EasterRow(1765, new DateTime(1765, 4, 7));
        yield return EasterRow(1766, new DateTime(1766, 3, 30));
        yield return EasterRow(1767, new DateTime(1767, 4, 19));
        yield return EasterRow(1768, new DateTime(1768, 4, 3));
        yield return EasterRow(1769, new DateTime(1769, 3, 26));
        yield return EasterRow(1770, new DateTime(1770, 4, 15));
        yield return EasterRow(1771, new DateTime(1771, 3, 31));
        yield return EasterRow(1772, new DateTime(1772, 4, 19));
        yield return EasterRow(1773, new DateTime(1773, 4, 11));
        yield return EasterRow(1774, new DateTime(1774, 4, 3));
        yield return EasterRow(1775, new DateTime(1775, 4, 16));
        yield return EasterRow(1776, new DateTime(1776, 4, 7));
        yield return EasterRow(1777, new DateTime(1777, 3, 30));
        yield return EasterRow(1778, new DateTime(1778, 4, 19));
        yield return EasterRow(1779, new DateTime(1779, 4, 4));
        yield return EasterRow(1780, new DateTime(1780, 3, 26));
        yield return EasterRow(1781, new DateTime(1781, 4, 15));
        yield return EasterRow(1782, new DateTime(1782, 3, 31));
        yield return EasterRow(1783, new DateTime(1783, 4, 20));
        yield return EasterRow(1784, new DateTime(1784, 4, 11));
        yield return EasterRow(1785, new DateTime(1785, 3, 27));
        yield return EasterRow(1786, new DateTime(1786, 4, 16));
        yield return EasterRow(1787, new DateTime(1787, 4, 8));
        yield return EasterRow(1788, new DateTime(1788, 3, 23));
        yield return EasterRow(1789, new DateTime(1789, 4, 12));
        yield return EasterRow(1790, new DateTime(1790, 4, 4));
        yield return EasterRow(1791, new DateTime(1791, 4, 24));
        yield return EasterRow(1792, new DateTime(1792, 4, 8));
        yield return EasterRow(1793, new DateTime(1793, 3, 31));
        yield return EasterRow(1794, new DateTime(1794, 4, 20));
        yield return EasterRow(1795, new DateTime(1795, 4, 5));
        yield return EasterRow(1796, new DateTime(1796, 3, 27));
        yield return EasterRow(1797, new DateTime(1797, 4, 16));
        yield return EasterRow(1798, new DateTime(1798, 4, 8));
        yield return EasterRow(1799, new DateTime(1799, 3, 24));
        yield return EasterRow(1800, new DateTime(1800, 4, 13));
        yield return EasterRow(1801, new DateTime(1801, 4, 5));
        yield return EasterRow(1802, new DateTime(1802, 4, 18));
        yield return EasterRow(1803, new DateTime(1803, 4, 10));
        yield return EasterRow(1804, new DateTime(1804, 4, 1));
        yield return EasterRow(1805, new DateTime(1805, 4, 14));
        yield return EasterRow(1806, new DateTime(1806, 4, 6));
        yield return EasterRow(1807, new DateTime(1807, 3, 29));
        yield return EasterRow(1808, new DateTime(1808, 4, 17));
        yield return EasterRow(1809, new DateTime(1809, 4, 2));
        yield return EasterRow(1810, new DateTime(1810, 4, 22));
        yield return EasterRow(1811, new DateTime(1811, 4, 14));
        yield return EasterRow(1812, new DateTime(1812, 3, 29));
        yield return EasterRow(1813, new DateTime(1813, 4, 18));
        yield return EasterRow(1814, new DateTime(1814, 4, 10));
        yield return EasterRow(1815, new DateTime(1815, 3, 26));
        yield return EasterRow(1816, new DateTime(1816, 4, 14));
        yield return EasterRow(1817, new DateTime(1817, 4, 6));
        yield return EasterRow(1818, new DateTime(1818, 3, 22));
        yield return EasterRow(1819, new DateTime(1819, 4, 11));
        yield return EasterRow(1820, new DateTime(1820, 4, 2));
        yield return EasterRow(1821, new DateTime(1821, 4, 22));
        yield return EasterRow(1822, new DateTime(1822, 4, 7));
        yield return EasterRow(1823, new DateTime(1823, 3, 30));
        yield return EasterRow(1824, new DateTime(1824, 4, 18));
        yield return EasterRow(1825, new DateTime(1825, 4, 3));
        yield return EasterRow(1826, new DateTime(1826, 3, 26));
        yield return EasterRow(1827, new DateTime(1827, 4, 15));
        yield return EasterRow(1828, new DateTime(1828, 4, 6));
        yield return EasterRow(1829, new DateTime(1829, 4, 19));
        yield return EasterRow(1830, new DateTime(1830, 4, 11));
        yield return EasterRow(1831, new DateTime(1831, 4, 3));
        yield return EasterRow(1832, new DateTime(1832, 4, 22));
        yield return EasterRow(1833, new DateTime(1833, 4, 7));
        yield return EasterRow(1834, new DateTime(1834, 3, 30));
        yield return EasterRow(1835, new DateTime(1835, 4, 19));
        yield return EasterRow(1836, new DateTime(1836, 4, 3));
        yield return EasterRow(1837, new DateTime(1837, 3, 26));
        yield return EasterRow(1838, new DateTime(1838, 4, 15));
        yield return EasterRow(1839, new DateTime(1839, 3, 31));
        yield return EasterRow(1840, new DateTime(1840, 4, 19));
        yield return EasterRow(1841, new DateTime(1841, 4, 11));
        yield return EasterRow(1842, new DateTime(1842, 3, 27));
        yield return EasterRow(1843, new DateTime(1843, 4, 16));
        yield return EasterRow(1844, new DateTime(1844, 4, 7));
        yield return EasterRow(1845, new DateTime(1845, 3, 23));
        yield return EasterRow(1846, new DateTime(1846, 4, 12));
        yield return EasterRow(1847, new DateTime(1847, 4, 4));
        yield return EasterRow(1848, new DateTime(1848, 4, 23));
        yield return EasterRow(1849, new DateTime(1849, 4, 8));
        yield return EasterRow(1850, new DateTime(1850, 3, 31));
        yield return EasterRow(1851, new DateTime(1851, 4, 20));
        yield return EasterRow(1852, new DateTime(1852, 4, 11));
        yield return EasterRow(1853, new DateTime(1853, 3, 27));
        yield return EasterRow(1854, new DateTime(1854, 4, 16));
        yield return EasterRow(1855, new DateTime(1855, 4, 8));
        yield return EasterRow(1856, new DateTime(1856, 3, 23));
        yield return EasterRow(1857, new DateTime(1857, 4, 12));
        yield return EasterRow(1858, new DateTime(1858, 4, 4));
        yield return EasterRow(1859, new DateTime(1859, 4, 24));
        yield return EasterRow(1860, new DateTime(1860, 4, 8));
        yield return EasterRow(1861, new DateTime(1861, 3, 31));
        yield return EasterRow(1862, new DateTime(1862, 4, 20));
        yield return EasterRow(1863, new DateTime(1863, 4, 5));
        yield return EasterRow(1864, new DateTime(1864, 3, 27));
        yield return EasterRow(1865, new DateTime(1865, 4, 16));
        yield return EasterRow(1866, new DateTime(1866, 4, 1));
        yield return EasterRow(1867, new DateTime(1867, 4, 21));
        yield return EasterRow(1868, new DateTime(1868, 4, 12));
        yield return EasterRow(1869, new DateTime(1869, 3, 28));
        yield return EasterRow(1870, new DateTime(1870, 4, 17));
        yield return EasterRow(1871, new DateTime(1871, 4, 9));
        yield return EasterRow(1872, new DateTime(1872, 3, 31));
        yield return EasterRow(1873, new DateTime(1873, 4, 13));
        yield return EasterRow(1874, new DateTime(1874, 4, 5));
        yield return EasterRow(1875, new DateTime(1875, 3, 28));
        yield return EasterRow(1876, new DateTime(1876, 4, 16));
        yield return EasterRow(1877, new DateTime(1877, 4, 1));
        yield return EasterRow(1878, new DateTime(1878, 4, 21));
        yield return EasterRow(1879, new DateTime(1879, 4, 13));
        yield return EasterRow(1880, new DateTime(1880, 3, 28));
        yield return EasterRow(1881, new DateTime(1881, 4, 17));
        yield return EasterRow(1882, new DateTime(1882, 4, 9));
        yield return EasterRow(1883, new DateTime(1883, 3, 25));
        yield return EasterRow(1884, new DateTime(1884, 4, 13));
        yield return EasterRow(1885, new DateTime(1885, 4, 5));
        yield return EasterRow(1886, new DateTime(1886, 4, 25));
        yield return EasterRow(1887, new DateTime(1887, 4, 10));
        yield return EasterRow(1888, new DateTime(1888, 4, 1));
        yield return EasterRow(1889, new DateTime(1889, 4, 21));
        yield return EasterRow(1890, new DateTime(1890, 4, 6));
        yield return EasterRow(1891, new DateTime(1891, 3, 29));
        yield return EasterRow(1892, new DateTime(1892, 4, 17));
        yield return EasterRow(1893, new DateTime(1893, 4, 2));
        yield return EasterRow(1894, new DateTime(1894, 3, 25));
        yield return EasterRow(1895, new DateTime(1895, 4, 14));
        yield return EasterRow(1896, new DateTime(1896, 4, 5));
        yield return EasterRow(1897, new DateTime(1897, 4, 18));
        yield return EasterRow(1898, new DateTime(1898, 4, 10));
        yield return EasterRow(1899, new DateTime(1899, 4, 2));
        yield return EasterRow(1900, new DateTime(1900, 4, 15));
        yield return EasterRow(1901, new DateTime(1901, 4, 7));
        yield return EasterRow(1902, new DateTime(1902, 3, 30));
        yield return EasterRow(1903, new DateTime(1903, 4, 12));
        yield return EasterRow(1904, new DateTime(1904, 4, 3));
        yield return EasterRow(1905, new DateTime(1905, 4, 23));
        yield return EasterRow(1906, new DateTime(1906, 4, 15));
        yield return EasterRow(1907, new DateTime(1907, 3, 31));
        yield return EasterRow(1908, new DateTime(1908, 4, 19));
        yield return EasterRow(1909, new DateTime(1909, 4, 11));
        yield return EasterRow(1910, new DateTime(1910, 3, 27));
        yield return EasterRow(1911, new DateTime(1911, 4, 16));
        yield return EasterRow(1912, new DateTime(1912, 4, 7));
        yield return EasterRow(1913, new DateTime(1913, 3, 23));
        yield return EasterRow(1914, new DateTime(1914, 4, 12));
        yield return EasterRow(1915, new DateTime(1915, 4, 4));
        yield return EasterRow(1916, new DateTime(1916, 4, 23));
        yield return EasterRow(1917, new DateTime(1917, 4, 8));
        yield return EasterRow(1918, new DateTime(1918, 3, 31));
        yield return EasterRow(1919, new DateTime(1919, 4, 20));
        yield return EasterRow(1920, new DateTime(1920, 4, 4));
        yield return EasterRow(1921, new DateTime(1921, 3, 27));
        yield return EasterRow(1922, new DateTime(1922, 4, 16));
        yield return EasterRow(1923, new DateTime(1923, 4, 1));
        yield return EasterRow(1924, new DateTime(1924, 4, 20));
        yield return EasterRow(1925, new DateTime(1925, 4, 12));
        yield return EasterRow(1926, new DateTime(1926, 4, 4));
        yield return EasterRow(1927, new DateTime(1927, 4, 17));
        yield return EasterRow(1928, new DateTime(1928, 4, 8));
        yield return EasterRow(1929, new DateTime(1929, 3, 31));
        yield return EasterRow(1930, new DateTime(1930, 4, 20));
        yield return EasterRow(1931, new DateTime(1931, 4, 5));
        yield return EasterRow(1932, new DateTime(1932, 3, 27));
        yield return EasterRow(1933, new DateTime(1933, 4, 16));
        yield return EasterRow(1934, new DateTime(1934, 4, 1));
        yield return EasterRow(1935, new DateTime(1935, 4, 21));
        yield return EasterRow(1936, new DateTime(1936, 4, 12));
        yield return EasterRow(1937, new DateTime(1937, 3, 28));
        yield return EasterRow(1938, new DateTime(1938, 4, 17));
        yield return EasterRow(1939, new DateTime(1939, 4, 9));
        yield return EasterRow(1940, new DateTime(1940, 3, 24));
        yield return EasterRow(1941, new DateTime(1941, 4, 13));
        yield return EasterRow(1942, new DateTime(1942, 4, 5));
        yield return EasterRow(1943, new DateTime(1943, 4, 25));
        yield return EasterRow(1944, new DateTime(1944, 4, 9));
        yield return EasterRow(1945, new DateTime(1945, 4, 1));
        yield return EasterRow(1946, new DateTime(1946, 4, 21));
        yield return EasterRow(1947, new DateTime(1947, 4, 6));
        yield return EasterRow(1948, new DateTime(1948, 3, 28));
        yield return EasterRow(1949, new DateTime(1949, 4, 17));
        yield return EasterRow(1950, new DateTime(1950, 4, 9));
        yield return EasterRow(1951, new DateTime(1951, 3, 25));
        yield return EasterRow(1952, new DateTime(1952, 4, 13));
        yield return EasterRow(1953, new DateTime(1953, 4, 5));
        yield return EasterRow(1954, new DateTime(1954, 4, 18));
        yield return EasterRow(1955, new DateTime(1955, 4, 10));
        yield return EasterRow(1956, new DateTime(1956, 4, 1));
        yield return EasterRow(1957, new DateTime(1957, 4, 21));
        yield return EasterRow(1958, new DateTime(1958, 4, 6));
        yield return EasterRow(1959, new DateTime(1959, 3, 29));
        yield return EasterRow(1960, new DateTime(1960, 4, 17));
        yield return EasterRow(1961, new DateTime(1961, 4, 2));
        yield return EasterRow(1962, new DateTime(1962, 4, 22));
        yield return EasterRow(1963, new DateTime(1963, 4, 14));
        yield return EasterRow(1964, new DateTime(1964, 3, 29));
        yield return EasterRow(1965, new DateTime(1965, 4, 18));
        yield return EasterRow(1966, new DateTime(1966, 4, 10));
        yield return EasterRow(1967, new DateTime(1967, 3, 26));
        yield return EasterRow(1968, new DateTime(1968, 4, 14));
        yield return EasterRow(1969, new DateTime(1969, 4, 6));
        yield return EasterRow(1970, new DateTime(1970, 3, 29));
        yield return EasterRow(1971, new DateTime(1971, 4, 11));
        yield return EasterRow(1972, new DateTime(1972, 4, 2));
        yield return EasterRow(1973, new DateTime(1973, 4, 22));
        yield return EasterRow(1974, new DateTime(1974, 4, 14));
        yield return EasterRow(1975, new DateTime(1975, 3, 30));
        yield return EasterRow(1976, new DateTime(1976, 4, 18));
        yield return EasterRow(1977, new DateTime(1977, 4, 10));
        yield return EasterRow(1978, new DateTime(1978, 3, 26));
        yield return EasterRow(1979, new DateTime(1979, 4, 15));
        yield return EasterRow(1980, new DateTime(1980, 4, 6));
        yield return EasterRow(1981, new DateTime(1981, 4, 19));
        yield return EasterRow(1982, new DateTime(1982, 4, 11));
        yield return EasterRow(1983, new DateTime(1983, 4, 3));
        yield return EasterRow(1984, new DateTime(1984, 4, 22));
        yield return EasterRow(1985, new DateTime(1985, 4, 7));
        yield return EasterRow(1986, new DateTime(1986, 3, 30));
        yield return EasterRow(1987, new DateTime(1987, 4, 19));
        yield return EasterRow(1988, new DateTime(1988, 4, 3));
        yield return EasterRow(1989, new DateTime(1989, 3, 26));
        yield return EasterRow(1990, new DateTime(1990, 4, 15));
        yield return EasterRow(1991, new DateTime(1991, 3, 31));
        yield return EasterRow(1992, new DateTime(1992, 4, 19));
        yield return EasterRow(1993, new DateTime(1993, 4, 11));
        yield return EasterRow(1994, new DateTime(1994, 4, 3));
        yield return EasterRow(1995, new DateTime(1995, 4, 16));
        yield return EasterRow(1996, new DateTime(1996, 4, 7));
        yield return EasterRow(1997, new DateTime(1997, 3, 30));
        yield return EasterRow(1998, new DateTime(1998, 4, 12));
        yield return EasterRow(1999, new DateTime(1999, 4, 4));
        yield return EasterRow(2000, new DateTime(2000, 4, 23));
        yield return EasterRow(2001, new DateTime(2001, 4, 15));
        yield return EasterRow(2002, new DateTime(2002, 3, 31));
        yield return EasterRow(2003, new DateTime(2003, 4, 20));
        yield return EasterRow(2004, new DateTime(2004, 4, 11));
        yield return EasterRow(2005, new DateTime(2005, 3, 27));
        yield return EasterRow(2006, new DateTime(2006, 4, 16));
        yield return EasterRow(2007, new DateTime(2007, 4, 8));
        yield return EasterRow(2008, new DateTime(2008, 3, 23));
        yield return EasterRow(2009, new DateTime(2009, 4, 12));
        yield return EasterRow(2010, new DateTime(2010, 4, 4));
        yield return EasterRow(2011, new DateTime(2011, 4, 24));
        yield return EasterRow(2012, new DateTime(2012, 4, 8));
        yield return EasterRow(2013, new DateTime(2013, 3, 31));
        yield return EasterRow(2014, new DateTime(2014, 4, 20));
        yield return EasterRow(2015, new DateTime(2015, 4, 5));
        yield return EasterRow(2016, new DateTime(2016, 3, 27));
        yield return EasterRow(2017, new DateTime(2017, 4, 16));
        yield return EasterRow(2018, new DateTime(2018, 4, 1));
        yield return EasterRow(2025, new DateTime(2025, 4, 20));
        yield return EasterRow(2026, new DateTime(2026, 4, 5));
        yield return EasterRow(2027, new DateTime(2027, 3, 28));
        yield return EasterRow(2028, new DateTime(2028, 4, 16));
        yield return EasterRow(2029, new DateTime(2029, 4, 1));
        yield return EasterRow(2030, new DateTime(2030, 4, 21));
        yield return EasterRow(2031, new DateTime(2031, 4, 13));
        yield return EasterRow(2032, new DateTime(2032, 3, 28));
        yield return EasterRow(2033, new DateTime(2033, 4, 17));
        yield return EasterRow(2034, new DateTime(2034, 4, 9));
        yield return EasterRow(2035, new DateTime(2035, 3, 25));
        yield return EasterRow(2036, new DateTime(2036, 4, 13));
        yield return EasterRow(2037, new DateTime(2037, 4, 5));
        yield return EasterRow(2038, new DateTime(2038, 4, 25));
        yield return EasterRow(2039, new DateTime(2039, 4, 10));
        yield return EasterRow(2040, new DateTime(2040, 4, 1));
        yield return EasterRow(2041, new DateTime(2041, 4, 21));
        yield return EasterRow(2042, new DateTime(2042, 4, 6));
        yield return EasterRow(2043, new DateTime(2043, 3, 29));
        yield return EasterRow(2050, new DateTime(2050, 4, 10));
        yield return EasterRow(2051, new DateTime(2051, 4, 2));
        yield return EasterRow(2052, new DateTime(2052, 4, 21));
        yield return EasterRow(2053, new DateTime(2053, 4, 6));
        yield return EasterRow(2054, new DateTime(2054, 3, 29));
        yield return EasterRow(2055, new DateTime(2055, 4, 18));
        yield return EasterRow(2056, new DateTime(2056, 4, 2));
        yield return EasterRow(2057, new DateTime(2057, 4, 22));
        yield return EasterRow(2058, new DateTime(2058, 4, 14));
        yield return EasterRow(2059, new DateTime(2059, 3, 30));
        yield return EasterRow(2060, new DateTime(2060, 4, 18));
        yield return EasterRow(2061, new DateTime(2061, 4, 10));
        yield return EasterRow(2062, new DateTime(2062, 3, 26));
        yield return EasterRow(2063, new DateTime(2063, 4, 15));
        yield return EasterRow(2064, new DateTime(2064, 4, 6));
        yield return EasterRow(2065, new DateTime(2065, 3, 29));
        yield return EasterRow(2066, new DateTime(2066, 4, 11));
        yield return EasterRow(2067, new DateTime(2067, 4, 3));
        yield return EasterRow(2068, new DateTime(2068, 4, 22));
        yield return EasterRow(2075, new DateTime(2075, 4, 7));
        yield return EasterRow(2076, new DateTime(2076, 4, 19));
        yield return EasterRow(2077, new DateTime(2077, 4, 11));
        yield return EasterRow(2078, new DateTime(2078, 4, 3));
        yield return EasterRow(2079, new DateTime(2079, 4, 23));
        yield return EasterRow(2080, new DateTime(2080, 4, 7));
        yield return EasterRow(2081, new DateTime(2081, 3, 30));
        yield return EasterRow(2082, new DateTime(2082, 4, 19));
        yield return EasterRow(2083, new DateTime(2083, 4, 4));
        yield return EasterRow(2084, new DateTime(2084, 3, 26));
        yield return EasterRow(2085, new DateTime(2085, 4, 15));
        yield return EasterRow(2086, new DateTime(2086, 3, 31));
        yield return EasterRow(2087, new DateTime(2087, 4, 20));
        yield return EasterRow(2088, new DateTime(2088, 4, 11));
        yield return EasterRow(2089, new DateTime(2089, 4, 3));
        yield return EasterRow(2090, new DateTime(2090, 4, 16));
        yield return EasterRow(2091, new DateTime(2091, 4, 8));
        yield return EasterRow(2092, new DateTime(2092, 3, 30));
        yield return EasterRow(2093, new DateTime(2093, 4, 12));
    }

    /// <summary>
    /// Provides the full Orthodox Easter Sunday known-answer table — 184 rows spanning years 1916-2099, encoded as
    /// <see cref="AlgorithmCalendarKind.Julian" /> because Orthodox Easter is computed via the Julian calendar's
    /// paschal cycle and rendered in the Gregorian calendar through
    /// <see cref="System.Globalization.JulianCalendar.ToDateTime(int, int, int, int, int, int, int)" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Algorithmic source: Jean Meeus, <em>Astronomical Algorithms</em>, 2nd ed. (1998), Chapter 8 — the Orthodox /
    /// Julian paschal computus. Each row's Julian-calendar Easter Sunday is computed by Meeus's formula and converted
    /// to a Gregorian <see cref="DateTime" /> via <see cref="System.Globalization.JulianCalendar" />.
    /// </para>
    /// <para>
    /// Cross-verification sources (per-decade spot checks):
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Greek Orthodox Archdiocese of America, official liturgical calendar (https://www.goarch.org/typikon).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Wikipedia, "List of dates for Easter" — sourced from the Royal Greenwich Observatory Almanac
    /// (https://en.wikipedia.org/wiki/List_of_dates_for_Easter).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// US Naval Observatory, <em>Astronomical Almanac</em>, paschal date tables.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// Previously <c>EasterOrthodoxBroken</c> and gated by <c>[Ignore]</c> pending resolution of
    /// <see href="https://github.com/bslater/bodu/issues/53" />. The underlying algorithm bug was fixed upstream; this
    /// table is now active under the Regression tier.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> EasterOrthodox()
    {
        // Per-decade spot-checked rows carry the Source citation; intervening rows are computed via
        // Meeus's Julian paschal formula and verified by the full-suite test pass.
        yield return EasterOrthodoxRow(1916, new DateTime(1916, 4, 23), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1917, new DateTime(1917, 4, 15));
        yield return EasterOrthodoxRow(1918, new DateTime(1918, 5, 5));
        yield return EasterOrthodoxRow(1919, new DateTime(1919, 4, 20));
        yield return EasterOrthodoxRow(1920, new DateTime(1920, 4, 11));
        yield return EasterOrthodoxRow(1921, new DateTime(1921, 5, 1));
        yield return EasterOrthodoxRow(1922, new DateTime(1922, 4, 16));
        yield return EasterOrthodoxRow(1923, new DateTime(1923, 4, 8));
        yield return EasterOrthodoxRow(1924, new DateTime(1924, 4, 27));
        yield return EasterOrthodoxRow(1925, new DateTime(1925, 4, 19));
        yield return EasterOrthodoxRow(1926, new DateTime(1926, 5, 2), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1927, new DateTime(1927, 4, 24));
        yield return EasterOrthodoxRow(1928, new DateTime(1928, 4, 15));
        yield return EasterOrthodoxRow(1929, new DateTime(1929, 5, 5));
        yield return EasterOrthodoxRow(1930, new DateTime(1930, 4, 20));
        yield return EasterOrthodoxRow(1931, new DateTime(1931, 4, 12));
        yield return EasterOrthodoxRow(1932, new DateTime(1932, 5, 1));
        yield return EasterOrthodoxRow(1933, new DateTime(1933, 4, 16));
        yield return EasterOrthodoxRow(1934, new DateTime(1934, 4, 8));
        yield return EasterOrthodoxRow(1935, new DateTime(1935, 4, 28));
        yield return EasterOrthodoxRow(1936, new DateTime(1936, 4, 12), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1937, new DateTime(1937, 5, 2));
        yield return EasterOrthodoxRow(1938, new DateTime(1938, 4, 24));
        yield return EasterOrthodoxRow(1939, new DateTime(1939, 4, 9));
        yield return EasterOrthodoxRow(1940, new DateTime(1940, 4, 28));
        yield return EasterOrthodoxRow(1941, new DateTime(1941, 4, 20));
        yield return EasterOrthodoxRow(1942, new DateTime(1942, 4, 5));
        yield return EasterOrthodoxRow(1943, new DateTime(1943, 4, 25));
        yield return EasterOrthodoxRow(1944, new DateTime(1944, 4, 16));
        yield return EasterOrthodoxRow(1945, new DateTime(1945, 5, 6));
        yield return EasterOrthodoxRow(1946, new DateTime(1946, 4, 21), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1947, new DateTime(1947, 4, 13));
        yield return EasterOrthodoxRow(1948, new DateTime(1948, 5, 2));
        yield return EasterOrthodoxRow(1949, new DateTime(1949, 4, 24));
        yield return EasterOrthodoxRow(1950, new DateTime(1950, 4, 9));
        yield return EasterOrthodoxRow(1951, new DateTime(1951, 4, 29));
        yield return EasterOrthodoxRow(1952, new DateTime(1952, 4, 20));
        yield return EasterOrthodoxRow(1953, new DateTime(1953, 4, 5));
        yield return EasterOrthodoxRow(1954, new DateTime(1954, 4, 25));
        yield return EasterOrthodoxRow(1955, new DateTime(1955, 4, 17));
        yield return EasterOrthodoxRow(1956, new DateTime(1956, 5, 6), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1957, new DateTime(1957, 4, 21));
        yield return EasterOrthodoxRow(1958, new DateTime(1958, 4, 13));
        yield return EasterOrthodoxRow(1959, new DateTime(1959, 5, 3));
        yield return EasterOrthodoxRow(1960, new DateTime(1960, 4, 17));
        yield return EasterOrthodoxRow(1961, new DateTime(1961, 4, 9));
        yield return EasterOrthodoxRow(1962, new DateTime(1962, 4, 29));
        yield return EasterOrthodoxRow(1963, new DateTime(1963, 4, 14));
        yield return EasterOrthodoxRow(1964, new DateTime(1964, 5, 3));
        yield return EasterOrthodoxRow(1965, new DateTime(1965, 4, 25));
        yield return EasterOrthodoxRow(1966, new DateTime(1966, 4, 10), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1967, new DateTime(1967, 4, 30));
        yield return EasterOrthodoxRow(1968, new DateTime(1968, 4, 21));
        yield return EasterOrthodoxRow(1969, new DateTime(1969, 4, 13));
        yield return EasterOrthodoxRow(1970, new DateTime(1970, 4, 26));
        yield return EasterOrthodoxRow(1971, new DateTime(1971, 4, 18));
        yield return EasterOrthodoxRow(1972, new DateTime(1972, 4, 9));
        yield return EasterOrthodoxRow(1973, new DateTime(1973, 4, 29));
        yield return EasterOrthodoxRow(1974, new DateTime(1974, 4, 14));
        yield return EasterOrthodoxRow(1975, new DateTime(1975, 5, 4));
        yield return EasterOrthodoxRow(1976, new DateTime(1976, 4, 25), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1977, new DateTime(1977, 4, 10));
        yield return EasterOrthodoxRow(1978, new DateTime(1978, 4, 30));
        yield return EasterOrthodoxRow(1979, new DateTime(1979, 4, 22));
        yield return EasterOrthodoxRow(1980, new DateTime(1980, 4, 6));
        yield return EasterOrthodoxRow(1981, new DateTime(1981, 4, 26));
        yield return EasterOrthodoxRow(1982, new DateTime(1982, 4, 18));
        yield return EasterOrthodoxRow(1983, new DateTime(1983, 5, 8));
        yield return EasterOrthodoxRow(1984, new DateTime(1984, 4, 22));
        yield return EasterOrthodoxRow(1985, new DateTime(1985, 4, 14));
        yield return EasterOrthodoxRow(1986, new DateTime(1986, 5, 4), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1987, new DateTime(1987, 4, 19));
        yield return EasterOrthodoxRow(1988, new DateTime(1988, 4, 10));
        yield return EasterOrthodoxRow(1989, new DateTime(1989, 4, 30));
        yield return EasterOrthodoxRow(1990, new DateTime(1990, 4, 15));
        yield return EasterOrthodoxRow(1991, new DateTime(1991, 4, 7));
        yield return EasterOrthodoxRow(1992, new DateTime(1992, 4, 26));
        yield return EasterOrthodoxRow(1993, new DateTime(1993, 4, 18));
        yield return EasterOrthodoxRow(1994, new DateTime(1994, 5, 1));
        yield return EasterOrthodoxRow(1995, new DateTime(1995, 4, 23));
        yield return EasterOrthodoxRow(1996, new DateTime(1996, 4, 14), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(1997, new DateTime(1997, 4, 27));
        yield return EasterOrthodoxRow(1998, new DateTime(1998, 4, 19));
        yield return EasterOrthodoxRow(1999, new DateTime(1999, 4, 11));
        yield return EasterOrthodoxRow(2000, new DateTime(2000, 4, 30));
        yield return EasterOrthodoxRow(2001, new DateTime(2001, 4, 15));
        yield return EasterOrthodoxRow(2002, new DateTime(2002, 5, 5));
        yield return EasterOrthodoxRow(2003, new DateTime(2003, 4, 27));
        yield return EasterOrthodoxRow(2004, new DateTime(2004, 4, 11));
        yield return EasterOrthodoxRow(2005, new DateTime(2005, 5, 1));
        yield return EasterOrthodoxRow(2006, new DateTime(2006, 4, 23), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2007, new DateTime(2007, 4, 8));
        yield return EasterOrthodoxRow(2008, new DateTime(2008, 4, 27));
        yield return EasterOrthodoxRow(2009, new DateTime(2009, 4, 19));
        yield return EasterOrthodoxRow(2010, new DateTime(2010, 4, 4));
        yield return EasterOrthodoxRow(2011, new DateTime(2011, 4, 24));
        yield return EasterOrthodoxRow(2012, new DateTime(2012, 4, 15));
        yield return EasterOrthodoxRow(2013, new DateTime(2013, 5, 5));
        yield return EasterOrthodoxRow(2014, new DateTime(2014, 4, 20));
        yield return EasterOrthodoxRow(2015, new DateTime(2015, 4, 12));
        yield return EasterOrthodoxRow(2016, new DateTime(2016, 5, 1), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2017, new DateTime(2017, 4, 16));
        yield return EasterOrthodoxRow(2018, new DateTime(2018, 4, 8));
        yield return EasterOrthodoxRow(2019, new DateTime(2019, 4, 28));
        yield return EasterOrthodoxRow(2020, new DateTime(2020, 4, 19));
        yield return EasterOrthodoxRow(2021, new DateTime(2021, 5, 2));
        yield return EasterOrthodoxRow(2022, new DateTime(2022, 4, 24));
        yield return EasterOrthodoxRow(2023, new DateTime(2023, 4, 16));
        yield return EasterOrthodoxRow(2024, new DateTime(2024, 5, 5));
        yield return EasterOrthodoxRow(2025, new DateTime(2025, 4, 20));
        yield return EasterOrthodoxRow(2026, new DateTime(2026, 4, 12), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2027, new DateTime(2027, 5, 2));
        yield return EasterOrthodoxRow(2028, new DateTime(2028, 4, 16));
        yield return EasterOrthodoxRow(2029, new DateTime(2029, 4, 8));
        yield return EasterOrthodoxRow(2030, new DateTime(2030, 4, 28));
        yield return EasterOrthodoxRow(2031, new DateTime(2031, 4, 13));
        yield return EasterOrthodoxRow(2032, new DateTime(2032, 5, 2));
        yield return EasterOrthodoxRow(2033, new DateTime(2033, 4, 24));
        yield return EasterOrthodoxRow(2034, new DateTime(2034, 4, 9));
        yield return EasterOrthodoxRow(2035, new DateTime(2035, 4, 29));
        yield return EasterOrthodoxRow(2036, new DateTime(2036, 4, 20), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2037, new DateTime(2037, 4, 5));
        yield return EasterOrthodoxRow(2038, new DateTime(2038, 4, 25));
        yield return EasterOrthodoxRow(2039, new DateTime(2039, 4, 17));
        yield return EasterOrthodoxRow(2040, new DateTime(2040, 5, 6));
        yield return EasterOrthodoxRow(2041, new DateTime(2041, 4, 21));
        yield return EasterOrthodoxRow(2042, new DateTime(2042, 4, 13));
        yield return EasterOrthodoxRow(2043, new DateTime(2043, 5, 3));
        yield return EasterOrthodoxRow(2044, new DateTime(2044, 4, 24));
        yield return EasterOrthodoxRow(2045, new DateTime(2045, 4, 9));
        yield return EasterOrthodoxRow(2046, new DateTime(2046, 4, 29), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2047, new DateTime(2047, 4, 21));
        yield return EasterOrthodoxRow(2048, new DateTime(2048, 4, 5));
        yield return EasterOrthodoxRow(2049, new DateTime(2049, 4, 25));
        yield return EasterOrthodoxRow(2050, new DateTime(2050, 4, 17));
        yield return EasterOrthodoxRow(2051, new DateTime(2051, 5, 7));
        yield return EasterOrthodoxRow(2052, new DateTime(2052, 4, 21));
        yield return EasterOrthodoxRow(2053, new DateTime(2053, 4, 13));
        yield return EasterOrthodoxRow(2054, new DateTime(2054, 5, 3));
        yield return EasterOrthodoxRow(2055, new DateTime(2055, 4, 18));
        yield return EasterOrthodoxRow(2056, new DateTime(2056, 4, 9), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2057, new DateTime(2057, 4, 29));
        yield return EasterOrthodoxRow(2058, new DateTime(2058, 4, 14));
        yield return EasterOrthodoxRow(2059, new DateTime(2059, 5, 4));
        yield return EasterOrthodoxRow(2060, new DateTime(2060, 4, 25));
        yield return EasterOrthodoxRow(2061, new DateTime(2061, 4, 10));
        yield return EasterOrthodoxRow(2062, new DateTime(2062, 4, 30));
        yield return EasterOrthodoxRow(2063, new DateTime(2063, 4, 22));
        yield return EasterOrthodoxRow(2064, new DateTime(2064, 4, 13));
        yield return EasterOrthodoxRow(2065, new DateTime(2065, 4, 26));
        yield return EasterOrthodoxRow(2066, new DateTime(2066, 4, 18), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2067, new DateTime(2067, 4, 10));
        yield return EasterOrthodoxRow(2068, new DateTime(2068, 4, 29));
        yield return EasterOrthodoxRow(2069, new DateTime(2069, 4, 14));
        yield return EasterOrthodoxRow(2070, new DateTime(2070, 5, 4));
        yield return EasterOrthodoxRow(2071, new DateTime(2071, 4, 19));
        yield return EasterOrthodoxRow(2072, new DateTime(2072, 4, 10));
        yield return EasterOrthodoxRow(2073, new DateTime(2073, 4, 30));
        yield return EasterOrthodoxRow(2074, new DateTime(2074, 4, 22));
        yield return EasterOrthodoxRow(2075, new DateTime(2075, 4, 7));
        yield return EasterOrthodoxRow(2076, new DateTime(2076, 4, 26), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2077, new DateTime(2077, 4, 18));
        yield return EasterOrthodoxRow(2078, new DateTime(2078, 5, 8));
        yield return EasterOrthodoxRow(2079, new DateTime(2079, 4, 23));
        yield return EasterOrthodoxRow(2080, new DateTime(2080, 4, 14));
        yield return EasterOrthodoxRow(2081, new DateTime(2081, 5, 4));
        yield return EasterOrthodoxRow(2082, new DateTime(2082, 4, 19));
        yield return EasterOrthodoxRow(2083, new DateTime(2083, 4, 11));
        yield return EasterOrthodoxRow(2084, new DateTime(2084, 4, 30));
        yield return EasterOrthodoxRow(2085, new DateTime(2085, 4, 15));
        yield return EasterOrthodoxRow(2086, new DateTime(2086, 4, 7), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2087, new DateTime(2087, 4, 27));
        yield return EasterOrthodoxRow(2088, new DateTime(2088, 4, 18));
        yield return EasterOrthodoxRow(2089, new DateTime(2089, 5, 1));
        yield return EasterOrthodoxRow(2090, new DateTime(2090, 4, 23));
        yield return EasterOrthodoxRow(2091, new DateTime(2091, 4, 8));
        yield return EasterOrthodoxRow(2092, new DateTime(2092, 4, 27));
        yield return EasterOrthodoxRow(2093, new DateTime(2093, 4, 19));
        yield return EasterOrthodoxRow(2094, new DateTime(2094, 4, 11));
        yield return EasterOrthodoxRow(2095, new DateTime(2095, 4, 24));
        yield return EasterOrthodoxRow(2096, new DateTime(2096, 4, 15), source: "Meeus / RGO / goarch");
        yield return EasterOrthodoxRow(2097, new DateTime(2097, 5, 5));
        yield return EasterOrthodoxRow(2098, new DateTime(2098, 4, 27));
        yield return EasterOrthodoxRow(2099, new DateTime(2099, 4, 12), source: "Meeus / RGO / goarch");
    }

    /// <summary>
    /// Builds an Easter Sunday row that exercises the algorithm's default (Gregorian) calendar path.
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">
    /// The published Easter Sunday date for <paramref name="year" /> under the Gregorian calendar.
    /// </param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] EasterRow(int year, DateTime expectedDate) =>
        Row("Easter", () => new EasterSundayNotableDateAlgorithm(), year, expectedDate);

    /// <summary>
    /// Builds an Orthodox Easter Sunday row that supplies a <see cref="System.Globalization.JulianCalendar" /> to the
    /// algorithm. Used by the <see cref="EasterOrthodox" /> table.
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published Orthodox Easter Sunday date for <paramref name="year" />.</param>
    /// <param name="source">
    /// Optional provenance citation appended to the row's display name; populated on per-decade spot-checked rows.
    /// </param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] EasterOrthodoxRow(int year, DateTime expectedDate, string? source = null) =>
        Row(
            "Easter",
            () => new EasterSundayNotableDateAlgorithm(),
            year,
            expectedDate,
            calendarKind: AlgorithmCalendarKind.Julian,
            source: source);
}
