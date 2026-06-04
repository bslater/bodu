// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies <see cref="AlgorithmDateStrategy" /> Western (Gregorian) and Orthodox (Julian) Easter Sunday computation
/// against the published year-by-year known-answer tables (1700–2093 Gregorian, 1916–2099 Julian), resolved through the
/// service so the algorithm dispatch and projection are exercised end to end.
/// </summary>
[TestClass]
public sealed class EasterKnownAnswerTests
{
    /// <summary>
    /// The service over the Easter fixture, which keys the Western and Orthodox computus algorithms.
    /// </summary>
    private static readonly NotableDateService s_service = new(NotableDateFixtures.Load("easter.xml"));

    /// <summary>
    /// Resolves the named Easter concept for a year and asserts its computed date.
    /// </summary>
    /// <param name="id">The Easter concept id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected month.</param>
    /// <param name="day">The expected day.</param>
    private static void AssertEaster(string id, int year, int month, int day)
    {
        NotableDate match = s_service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Single(r => r.NotableDateId == id);

        Assert.AreEqual(new DateOnly(year, month, day), match.Date);
    }

    /// <summary>
    /// Provides the Western (Gregorian) Easter Sunday known-answer rows, 1700–2093.
    /// </summary>
    /// <returns>Rows of <c>{ year, month, day }</c>.</returns>
    public static IEnumerable<object[]> GregorianRows()
    {
        yield return new object[] { 1700, 4, 11 };
        yield return new object[] { 1900, 4, 15 };
        yield return new object[] { 2000, 4, 23 };
        yield return new object[] { 2024, 3, 31 };
        yield return new object[] { 2030, 4, 21 };
        yield return new object[] { 1701, 3, 27 };
        yield return new object[] { 1702, 4, 16 };
        yield return new object[] { 1703, 4, 8 };
        yield return new object[] { 1704, 3, 23 };
        yield return new object[] { 1705, 4, 12 };
        yield return new object[] { 1706, 4, 4 };
        yield return new object[] { 1707, 4, 24 };
        yield return new object[] { 1708, 4, 8 };
        yield return new object[] { 1709, 3, 31 };
        yield return new object[] { 1710, 4, 20 };
        yield return new object[] { 1711, 4, 5 };
        yield return new object[] { 1712, 3, 27 };
        yield return new object[] { 1713, 4, 16 };
        yield return new object[] { 1714, 4, 1 };
        yield return new object[] { 1715, 4, 21 };
        yield return new object[] { 1716, 4, 12 };
        yield return new object[] { 1717, 3, 28 };
        yield return new object[] { 1718, 4, 17 };
        yield return new object[] { 1719, 4, 9 };
        yield return new object[] { 1720, 3, 31 };
        yield return new object[] { 1721, 4, 13 };
        yield return new object[] { 1722, 4, 5 };
        yield return new object[] { 1723, 3, 28 };
        yield return new object[] { 1724, 4, 16 };
        yield return new object[] { 1725, 4, 1 };
        yield return new object[] { 1726, 4, 21 };
        yield return new object[] { 1727, 4, 13 };
        yield return new object[] { 1728, 3, 28 };
        yield return new object[] { 1729, 4, 17 };
        yield return new object[] { 1730, 4, 9 };
        yield return new object[] { 1731, 3, 25 };
        yield return new object[] { 1732, 4, 13 };
        yield return new object[] { 1733, 4, 5 };
        yield return new object[] { 1734, 4, 25 };
        yield return new object[] { 1735, 4, 10 };
        yield return new object[] { 1736, 4, 1 };
        yield return new object[] { 1737, 4, 21 };
        yield return new object[] { 1738, 4, 6 };
        yield return new object[] { 1739, 3, 29 };
        yield return new object[] { 1740, 4, 17 };
        yield return new object[] { 1741, 4, 2 };
        yield return new object[] { 1742, 3, 25 };
        yield return new object[] { 1743, 4, 14 };
        yield return new object[] { 1744, 4, 5 };
        yield return new object[] { 1745, 4, 18 };
        yield return new object[] { 1746, 4, 10 };
        yield return new object[] { 1747, 4, 2 };
        yield return new object[] { 1748, 4, 14 };
        yield return new object[] { 1749, 4, 6 };
        yield return new object[] { 1750, 3, 29 };
        yield return new object[] { 1751, 4, 11 };
        yield return new object[] { 1752, 4, 2 };
        yield return new object[] { 1753, 4, 22 };
        yield return new object[] { 1754, 4, 14 };
        yield return new object[] { 1755, 3, 30 };
        yield return new object[] { 1756, 4, 18 };
        yield return new object[] { 1757, 4, 10 };
        yield return new object[] { 1758, 3, 26 };
        yield return new object[] { 1759, 4, 15 };
        yield return new object[] { 1760, 4, 6 };
        yield return new object[] { 1761, 3, 22 };
        yield return new object[] { 1762, 4, 11 };
        yield return new object[] { 1763, 4, 3 };
        yield return new object[] { 1764, 4, 22 };
        yield return new object[] { 1765, 4, 7 };
        yield return new object[] { 1766, 3, 30 };
        yield return new object[] { 1767, 4, 19 };
        yield return new object[] { 1768, 4, 3 };
        yield return new object[] { 1769, 3, 26 };
        yield return new object[] { 1770, 4, 15 };
        yield return new object[] { 1771, 3, 31 };
        yield return new object[] { 1772, 4, 19 };
        yield return new object[] { 1773, 4, 11 };
        yield return new object[] { 1774, 4, 3 };
        yield return new object[] { 1775, 4, 16 };
        yield return new object[] { 1776, 4, 7 };
        yield return new object[] { 1777, 3, 30 };
        yield return new object[] { 1778, 4, 19 };
        yield return new object[] { 1779, 4, 4 };
        yield return new object[] { 1780, 3, 26 };
        yield return new object[] { 1781, 4, 15 };
        yield return new object[] { 1782, 3, 31 };
        yield return new object[] { 1783, 4, 20 };
        yield return new object[] { 1784, 4, 11 };
        yield return new object[] { 1785, 3, 27 };
        yield return new object[] { 1786, 4, 16 };
        yield return new object[] { 1787, 4, 8 };
        yield return new object[] { 1788, 3, 23 };
        yield return new object[] { 1789, 4, 12 };
        yield return new object[] { 1790, 4, 4 };
        yield return new object[] { 1791, 4, 24 };
        yield return new object[] { 1792, 4, 8 };
        yield return new object[] { 1793, 3, 31 };
        yield return new object[] { 1794, 4, 20 };
        yield return new object[] { 1795, 4, 5 };
        yield return new object[] { 1796, 3, 27 };
        yield return new object[] { 1797, 4, 16 };
        yield return new object[] { 1798, 4, 8 };
        yield return new object[] { 1799, 3, 24 };
        yield return new object[] { 1800, 4, 13 };
        yield return new object[] { 1801, 4, 5 };
        yield return new object[] { 1802, 4, 18 };
        yield return new object[] { 1803, 4, 10 };
        yield return new object[] { 1804, 4, 1 };
        yield return new object[] { 1805, 4, 14 };
        yield return new object[] { 1806, 4, 6 };
        yield return new object[] { 1807, 3, 29 };
        yield return new object[] { 1808, 4, 17 };
        yield return new object[] { 1809, 4, 2 };
        yield return new object[] { 1810, 4, 22 };
        yield return new object[] { 1811, 4, 14 };
        yield return new object[] { 1812, 3, 29 };
        yield return new object[] { 1813, 4, 18 };
        yield return new object[] { 1814, 4, 10 };
        yield return new object[] { 1815, 3, 26 };
        yield return new object[] { 1816, 4, 14 };
        yield return new object[] { 1817, 4, 6 };
        yield return new object[] { 1818, 3, 22 };
        yield return new object[] { 1819, 4, 11 };
        yield return new object[] { 1820, 4, 2 };
        yield return new object[] { 1821, 4, 22 };
        yield return new object[] { 1822, 4, 7 };
        yield return new object[] { 1823, 3, 30 };
        yield return new object[] { 1824, 4, 18 };
        yield return new object[] { 1825, 4, 3 };
        yield return new object[] { 1826, 3, 26 };
        yield return new object[] { 1827, 4, 15 };
        yield return new object[] { 1828, 4, 6 };
        yield return new object[] { 1829, 4, 19 };
        yield return new object[] { 1830, 4, 11 };
        yield return new object[] { 1831, 4, 3 };
        yield return new object[] { 1832, 4, 22 };
        yield return new object[] { 1833, 4, 7 };
        yield return new object[] { 1834, 3, 30 };
        yield return new object[] { 1835, 4, 19 };
        yield return new object[] { 1836, 4, 3 };
        yield return new object[] { 1837, 3, 26 };
        yield return new object[] { 1838, 4, 15 };
        yield return new object[] { 1839, 3, 31 };
        yield return new object[] { 1840, 4, 19 };
        yield return new object[] { 1841, 4, 11 };
        yield return new object[] { 1842, 3, 27 };
        yield return new object[] { 1843, 4, 16 };
        yield return new object[] { 1844, 4, 7 };
        yield return new object[] { 1845, 3, 23 };
        yield return new object[] { 1846, 4, 12 };
        yield return new object[] { 1847, 4, 4 };
        yield return new object[] { 1848, 4, 23 };
        yield return new object[] { 1849, 4, 8 };
        yield return new object[] { 1850, 3, 31 };
        yield return new object[] { 1851, 4, 20 };
        yield return new object[] { 1852, 4, 11 };
        yield return new object[] { 1853, 3, 27 };
        yield return new object[] { 1854, 4, 16 };
        yield return new object[] { 1855, 4, 8 };
        yield return new object[] { 1856, 3, 23 };
        yield return new object[] { 1857, 4, 12 };
        yield return new object[] { 1858, 4, 4 };
        yield return new object[] { 1859, 4, 24 };
        yield return new object[] { 1860, 4, 8 };
        yield return new object[] { 1861, 3, 31 };
        yield return new object[] { 1862, 4, 20 };
        yield return new object[] { 1863, 4, 5 };
        yield return new object[] { 1864, 3, 27 };
        yield return new object[] { 1865, 4, 16 };
        yield return new object[] { 1866, 4, 1 };
        yield return new object[] { 1867, 4, 21 };
        yield return new object[] { 1868, 4, 12 };
        yield return new object[] { 1869, 3, 28 };
        yield return new object[] { 1870, 4, 17 };
        yield return new object[] { 1871, 4, 9 };
        yield return new object[] { 1872, 3, 31 };
        yield return new object[] { 1873, 4, 13 };
        yield return new object[] { 1874, 4, 5 };
        yield return new object[] { 1875, 3, 28 };
        yield return new object[] { 1876, 4, 16 };
        yield return new object[] { 1877, 4, 1 };
        yield return new object[] { 1878, 4, 21 };
        yield return new object[] { 1879, 4, 13 };
        yield return new object[] { 1880, 3, 28 };
        yield return new object[] { 1881, 4, 17 };
        yield return new object[] { 1882, 4, 9 };
        yield return new object[] { 1883, 3, 25 };
        yield return new object[] { 1884, 4, 13 };
        yield return new object[] { 1885, 4, 5 };
        yield return new object[] { 1886, 4, 25 };
        yield return new object[] { 1887, 4, 10 };
        yield return new object[] { 1888, 4, 1 };
        yield return new object[] { 1889, 4, 21 };
        yield return new object[] { 1890, 4, 6 };
        yield return new object[] { 1891, 3, 29 };
        yield return new object[] { 1892, 4, 17 };
        yield return new object[] { 1893, 4, 2 };
        yield return new object[] { 1894, 3, 25 };
        yield return new object[] { 1895, 4, 14 };
        yield return new object[] { 1896, 4, 5 };
        yield return new object[] { 1897, 4, 18 };
        yield return new object[] { 1898, 4, 10 };
        yield return new object[] { 1899, 4, 2 };
        yield return new object[] { 1901, 4, 7 };
        yield return new object[] { 1902, 3, 30 };
        yield return new object[] { 1903, 4, 12 };
        yield return new object[] { 1904, 4, 3 };
        yield return new object[] { 1905, 4, 23 };
        yield return new object[] { 1906, 4, 15 };
        yield return new object[] { 1907, 3, 31 };
        yield return new object[] { 1908, 4, 19 };
        yield return new object[] { 1909, 4, 11 };
        yield return new object[] { 1910, 3, 27 };
        yield return new object[] { 1911, 4, 16 };
        yield return new object[] { 1912, 4, 7 };
        yield return new object[] { 1913, 3, 23 };
        yield return new object[] { 1914, 4, 12 };
        yield return new object[] { 1915, 4, 4 };
        yield return new object[] { 1916, 4, 23 };
        yield return new object[] { 1917, 4, 8 };
        yield return new object[] { 1918, 3, 31 };
        yield return new object[] { 1919, 4, 20 };
        yield return new object[] { 1920, 4, 4 };
        yield return new object[] { 1921, 3, 27 };
        yield return new object[] { 1922, 4, 16 };
        yield return new object[] { 1923, 4, 1 };
        yield return new object[] { 1924, 4, 20 };
        yield return new object[] { 1925, 4, 12 };
        yield return new object[] { 1926, 4, 4 };
        yield return new object[] { 1927, 4, 17 };
        yield return new object[] { 1928, 4, 8 };
        yield return new object[] { 1929, 3, 31 };
        yield return new object[] { 1930, 4, 20 };
        yield return new object[] { 1931, 4, 5 };
        yield return new object[] { 1932, 3, 27 };
        yield return new object[] { 1933, 4, 16 };
        yield return new object[] { 1934, 4, 1 };
        yield return new object[] { 1935, 4, 21 };
        yield return new object[] { 1936, 4, 12 };
        yield return new object[] { 1937, 3, 28 };
        yield return new object[] { 1938, 4, 17 };
        yield return new object[] { 1939, 4, 9 };
        yield return new object[] { 1940, 3, 24 };
        yield return new object[] { 1941, 4, 13 };
        yield return new object[] { 1942, 4, 5 };
        yield return new object[] { 1943, 4, 25 };
        yield return new object[] { 1944, 4, 9 };
        yield return new object[] { 1945, 4, 1 };
        yield return new object[] { 1946, 4, 21 };
        yield return new object[] { 1947, 4, 6 };
        yield return new object[] { 1948, 3, 28 };
        yield return new object[] { 1949, 4, 17 };
        yield return new object[] { 1950, 4, 9 };
        yield return new object[] { 1951, 3, 25 };
        yield return new object[] { 1952, 4, 13 };
        yield return new object[] { 1953, 4, 5 };
        yield return new object[] { 1954, 4, 18 };
        yield return new object[] { 1955, 4, 10 };
        yield return new object[] { 1956, 4, 1 };
        yield return new object[] { 1957, 4, 21 };
        yield return new object[] { 1958, 4, 6 };
        yield return new object[] { 1959, 3, 29 };
        yield return new object[] { 1960, 4, 17 };
        yield return new object[] { 1961, 4, 2 };
        yield return new object[] { 1962, 4, 22 };
        yield return new object[] { 1963, 4, 14 };
        yield return new object[] { 1964, 3, 29 };
        yield return new object[] { 1965, 4, 18 };
        yield return new object[] { 1966, 4, 10 };
        yield return new object[] { 1967, 3, 26 };
        yield return new object[] { 1968, 4, 14 };
        yield return new object[] { 1969, 4, 6 };
        yield return new object[] { 1970, 3, 29 };
        yield return new object[] { 1971, 4, 11 };
        yield return new object[] { 1972, 4, 2 };
        yield return new object[] { 1973, 4, 22 };
        yield return new object[] { 1974, 4, 14 };
        yield return new object[] { 1975, 3, 30 };
        yield return new object[] { 1976, 4, 18 };
        yield return new object[] { 1977, 4, 10 };
        yield return new object[] { 1978, 3, 26 };
        yield return new object[] { 1979, 4, 15 };
        yield return new object[] { 1980, 4, 6 };
        yield return new object[] { 1981, 4, 19 };
        yield return new object[] { 1982, 4, 11 };
        yield return new object[] { 1983, 4, 3 };
        yield return new object[] { 1984, 4, 22 };
        yield return new object[] { 1985, 4, 7 };
        yield return new object[] { 1986, 3, 30 };
        yield return new object[] { 1987, 4, 19 };
        yield return new object[] { 1988, 4, 3 };
        yield return new object[] { 1989, 3, 26 };
        yield return new object[] { 1990, 4, 15 };
        yield return new object[] { 1991, 3, 31 };
        yield return new object[] { 1992, 4, 19 };
        yield return new object[] { 1993, 4, 11 };
        yield return new object[] { 1994, 4, 3 };
        yield return new object[] { 1995, 4, 16 };
        yield return new object[] { 1996, 4, 7 };
        yield return new object[] { 1997, 3, 30 };
        yield return new object[] { 1998, 4, 12 };
        yield return new object[] { 1999, 4, 4 };
        yield return new object[] { 2001, 4, 15 };
        yield return new object[] { 2002, 3, 31 };
        yield return new object[] { 2003, 4, 20 };
        yield return new object[] { 2004, 4, 11 };
        yield return new object[] { 2005, 3, 27 };
        yield return new object[] { 2006, 4, 16 };
        yield return new object[] { 2007, 4, 8 };
        yield return new object[] { 2008, 3, 23 };
        yield return new object[] { 2009, 4, 12 };
        yield return new object[] { 2010, 4, 4 };
        yield return new object[] { 2011, 4, 24 };
        yield return new object[] { 2012, 4, 8 };
        yield return new object[] { 2013, 3, 31 };
        yield return new object[] { 2014, 4, 20 };
        yield return new object[] { 2015, 4, 5 };
        yield return new object[] { 2016, 3, 27 };
        yield return new object[] { 2017, 4, 16 };
        yield return new object[] { 2018, 4, 1 };
        yield return new object[] { 2025, 4, 20 };
        yield return new object[] { 2026, 4, 5 };
        yield return new object[] { 2027, 3, 28 };
        yield return new object[] { 2028, 4, 16 };
        yield return new object[] { 2029, 4, 1 };
        yield return new object[] { 2031, 4, 13 };
        yield return new object[] { 2032, 3, 28 };
        yield return new object[] { 2033, 4, 17 };
        yield return new object[] { 2034, 4, 9 };
        yield return new object[] { 2035, 3, 25 };
        yield return new object[] { 2036, 4, 13 };
        yield return new object[] { 2037, 4, 5 };
        yield return new object[] { 2038, 4, 25 };
        yield return new object[] { 2039, 4, 10 };
        yield return new object[] { 2040, 4, 1 };
        yield return new object[] { 2041, 4, 21 };
        yield return new object[] { 2042, 4, 6 };
        yield return new object[] { 2043, 3, 29 };
        yield return new object[] { 2050, 4, 10 };
        yield return new object[] { 2051, 4, 2 };
        yield return new object[] { 2052, 4, 21 };
        yield return new object[] { 2053, 4, 6 };
        yield return new object[] { 2054, 3, 29 };
        yield return new object[] { 2055, 4, 18 };
        yield return new object[] { 2056, 4, 2 };
        yield return new object[] { 2057, 4, 22 };
        yield return new object[] { 2058, 4, 14 };
        yield return new object[] { 2059, 3, 30 };
        yield return new object[] { 2060, 4, 18 };
        yield return new object[] { 2061, 4, 10 };
        yield return new object[] { 2062, 3, 26 };
        yield return new object[] { 2063, 4, 15 };
        yield return new object[] { 2064, 4, 6 };
        yield return new object[] { 2065, 3, 29 };
        yield return new object[] { 2066, 4, 11 };
        yield return new object[] { 2067, 4, 3 };
        yield return new object[] { 2068, 4, 22 };
        yield return new object[] { 2075, 4, 7 };
        yield return new object[] { 2076, 4, 19 };
        yield return new object[] { 2077, 4, 11 };
        yield return new object[] { 2078, 4, 3 };
        yield return new object[] { 2079, 4, 23 };
        yield return new object[] { 2080, 4, 7 };
        yield return new object[] { 2081, 3, 30 };
        yield return new object[] { 2082, 4, 19 };
        yield return new object[] { 2083, 4, 4 };
        yield return new object[] { 2084, 3, 26 };
        yield return new object[] { 2085, 4, 15 };
        yield return new object[] { 2086, 3, 31 };
        yield return new object[] { 2087, 4, 20 };
        yield return new object[] { 2088, 4, 11 };
        yield return new object[] { 2089, 4, 3 };
        yield return new object[] { 2090, 4, 16 };
        yield return new object[] { 2091, 4, 8 };
        yield return new object[] { 2092, 3, 30 };
        yield return new object[] { 2093, 4, 12 };
    }

    /// <summary>
    /// Provides the Orthodox (Julian-paschal) Easter Sunday known-answer rows, 1916–2099.
    /// </summary>
    /// <returns>Rows of <c>{ year, month, day }</c>.</returns>
    public static IEnumerable<object[]> JulianRows()
    {
        yield return new object[] { 1916, 4, 23 };
        yield return new object[] { 1917, 4, 15 };
        yield return new object[] { 1918, 5, 5 };
        yield return new object[] { 1919, 4, 20 };
        yield return new object[] { 1920, 4, 11 };
        yield return new object[] { 1921, 5, 1 };
        yield return new object[] { 1922, 4, 16 };
        yield return new object[] { 1923, 4, 8 };
        yield return new object[] { 1924, 4, 27 };
        yield return new object[] { 1925, 4, 19 };
        yield return new object[] { 1926, 5, 2 };
        yield return new object[] { 1927, 4, 24 };
        yield return new object[] { 1928, 4, 15 };
        yield return new object[] { 1929, 5, 5 };
        yield return new object[] { 1930, 4, 20 };
        yield return new object[] { 1931, 4, 12 };
        yield return new object[] { 1932, 5, 1 };
        yield return new object[] { 1933, 4, 16 };
        yield return new object[] { 1934, 4, 8 };
        yield return new object[] { 1935, 4, 28 };
        yield return new object[] { 1936, 4, 12 };
        yield return new object[] { 1937, 5, 2 };
        yield return new object[] { 1938, 4, 24 };
        yield return new object[] { 1939, 4, 9 };
        yield return new object[] { 1940, 4, 28 };
        yield return new object[] { 1941, 4, 20 };
        yield return new object[] { 1942, 4, 5 };
        yield return new object[] { 1943, 4, 25 };
        yield return new object[] { 1944, 4, 16 };
        yield return new object[] { 1945, 5, 6 };
        yield return new object[] { 1946, 4, 21 };
        yield return new object[] { 1947, 4, 13 };
        yield return new object[] { 1948, 5, 2 };
        yield return new object[] { 1949, 4, 24 };
        yield return new object[] { 1950, 4, 9 };
        yield return new object[] { 1951, 4, 29 };
        yield return new object[] { 1952, 4, 20 };
        yield return new object[] { 1953, 4, 5 };
        yield return new object[] { 1954, 4, 25 };
        yield return new object[] { 1955, 4, 17 };
        yield return new object[] { 1956, 5, 6 };
        yield return new object[] { 1957, 4, 21 };
        yield return new object[] { 1958, 4, 13 };
        yield return new object[] { 1959, 5, 3 };
        yield return new object[] { 1960, 4, 17 };
        yield return new object[] { 1961, 4, 9 };
        yield return new object[] { 1962, 4, 29 };
        yield return new object[] { 1963, 4, 14 };
        yield return new object[] { 1964, 5, 3 };
        yield return new object[] { 1965, 4, 25 };
        yield return new object[] { 1966, 4, 10 };
        yield return new object[] { 1967, 4, 30 };
        yield return new object[] { 1968, 4, 21 };
        yield return new object[] { 1969, 4, 13 };
        yield return new object[] { 1970, 4, 26 };
        yield return new object[] { 1971, 4, 18 };
        yield return new object[] { 1972, 4, 9 };
        yield return new object[] { 1973, 4, 29 };
        yield return new object[] { 1974, 4, 14 };
        yield return new object[] { 1975, 5, 4 };
        yield return new object[] { 1976, 4, 25 };
        yield return new object[] { 1977, 4, 10 };
        yield return new object[] { 1978, 4, 30 };
        yield return new object[] { 1979, 4, 22 };
        yield return new object[] { 1980, 4, 6 };
        yield return new object[] { 1981, 4, 26 };
        yield return new object[] { 1982, 4, 18 };
        yield return new object[] { 1983, 5, 8 };
        yield return new object[] { 1984, 4, 22 };
        yield return new object[] { 1985, 4, 14 };
        yield return new object[] { 1986, 5, 4 };
        yield return new object[] { 1987, 4, 19 };
        yield return new object[] { 1988, 4, 10 };
        yield return new object[] { 1989, 4, 30 };
        yield return new object[] { 1990, 4, 15 };
        yield return new object[] { 1991, 4, 7 };
        yield return new object[] { 1992, 4, 26 };
        yield return new object[] { 1993, 4, 18 };
        yield return new object[] { 1994, 5, 1 };
        yield return new object[] { 1995, 4, 23 };
        yield return new object[] { 1996, 4, 14 };
        yield return new object[] { 1997, 4, 27 };
        yield return new object[] { 1998, 4, 19 };
        yield return new object[] { 1999, 4, 11 };
        yield return new object[] { 2000, 4, 30 };
        yield return new object[] { 2001, 4, 15 };
        yield return new object[] { 2002, 5, 5 };
        yield return new object[] { 2003, 4, 27 };
        yield return new object[] { 2004, 4, 11 };
        yield return new object[] { 2005, 5, 1 };
        yield return new object[] { 2006, 4, 23 };
        yield return new object[] { 2007, 4, 8 };
        yield return new object[] { 2008, 4, 27 };
        yield return new object[] { 2009, 4, 19 };
        yield return new object[] { 2010, 4, 4 };
        yield return new object[] { 2011, 4, 24 };
        yield return new object[] { 2012, 4, 15 };
        yield return new object[] { 2013, 5, 5 };
        yield return new object[] { 2014, 4, 20 };
        yield return new object[] { 2015, 4, 12 };
        yield return new object[] { 2016, 5, 1 };
        yield return new object[] { 2017, 4, 16 };
        yield return new object[] { 2018, 4, 8 };
        yield return new object[] { 2019, 4, 28 };
        yield return new object[] { 2020, 4, 19 };
        yield return new object[] { 2021, 5, 2 };
        yield return new object[] { 2022, 4, 24 };
        yield return new object[] { 2023, 4, 16 };
        yield return new object[] { 2024, 5, 5 };
        yield return new object[] { 2025, 4, 20 };
        yield return new object[] { 2026, 4, 12 };
        yield return new object[] { 2027, 5, 2 };
        yield return new object[] { 2028, 4, 16 };
        yield return new object[] { 2029, 4, 8 };
        yield return new object[] { 2030, 4, 28 };
        yield return new object[] { 2031, 4, 13 };
        yield return new object[] { 2032, 5, 2 };
        yield return new object[] { 2033, 4, 24 };
        yield return new object[] { 2034, 4, 9 };
        yield return new object[] { 2035, 4, 29 };
        yield return new object[] { 2036, 4, 20 };
        yield return new object[] { 2037, 4, 5 };
        yield return new object[] { 2038, 4, 25 };
        yield return new object[] { 2039, 4, 17 };
        yield return new object[] { 2040, 5, 6 };
        yield return new object[] { 2041, 4, 21 };
        yield return new object[] { 2042, 4, 13 };
        yield return new object[] { 2043, 5, 3 };
        yield return new object[] { 2044, 4, 24 };
        yield return new object[] { 2045, 4, 9 };
        yield return new object[] { 2046, 4, 29 };
        yield return new object[] { 2047, 4, 21 };
        yield return new object[] { 2048, 4, 5 };
        yield return new object[] { 2049, 4, 25 };
        yield return new object[] { 2050, 4, 17 };
        yield return new object[] { 2051, 5, 7 };
        yield return new object[] { 2052, 4, 21 };
        yield return new object[] { 2053, 4, 13 };
        yield return new object[] { 2054, 5, 3 };
        yield return new object[] { 2055, 4, 18 };
        yield return new object[] { 2056, 4, 9 };
        yield return new object[] { 2057, 4, 29 };
        yield return new object[] { 2058, 4, 14 };
        yield return new object[] { 2059, 5, 4 };
        yield return new object[] { 2060, 4, 25 };
        yield return new object[] { 2061, 4, 10 };
        yield return new object[] { 2062, 4, 30 };
        yield return new object[] { 2063, 4, 22 };
        yield return new object[] { 2064, 4, 13 };
        yield return new object[] { 2065, 4, 26 };
        yield return new object[] { 2066, 4, 18 };
        yield return new object[] { 2067, 4, 10 };
        yield return new object[] { 2068, 4, 29 };
        yield return new object[] { 2069, 4, 14 };
        yield return new object[] { 2070, 5, 4 };
        yield return new object[] { 2071, 4, 19 };
        yield return new object[] { 2072, 4, 10 };
        yield return new object[] { 2073, 4, 30 };
        yield return new object[] { 2074, 4, 22 };
        yield return new object[] { 2075, 4, 7 };
        yield return new object[] { 2076, 4, 26 };
        yield return new object[] { 2077, 4, 18 };
        yield return new object[] { 2078, 5, 8 };
        yield return new object[] { 2079, 4, 23 };
        yield return new object[] { 2080, 4, 14 };
        yield return new object[] { 2081, 5, 4 };
        yield return new object[] { 2082, 4, 19 };
        yield return new object[] { 2083, 4, 11 };
        yield return new object[] { 2084, 4, 30 };
        yield return new object[] { 2085, 4, 15 };
        yield return new object[] { 2086, 4, 7 };
        yield return new object[] { 2087, 4, 27 };
        yield return new object[] { 2088, 4, 18 };
        yield return new object[] { 2089, 5, 1 };
        yield return new object[] { 2090, 4, 23 };
        yield return new object[] { 2091, 4, 8 };
        yield return new object[] { 2092, 4, 27 };
        yield return new object[] { 2093, 4, 19 };
        yield return new object[] { 2094, 4, 11 };
        yield return new object[] { 2095, 4, 24 };
        yield return new object[] { 2096, 4, 15 };
        yield return new object[] { 2097, 5, 5 };
        yield return new object[] { 2098, 4, 27 };
        yield return new object[] { 2099, 4, 12 };
    }

    /// <summary>
    /// Verifies Western (Gregorian) Easter Sunday for every year in the known-answer table.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected month.</param>
    /// <param name="day">The expected day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(GregorianRows), DynamicDataSourceType.Method)]
    public void WesternEaster_MatchesKnownAnswer(int year, int month, int day) =>
        AssertEaster("western-easter", year, month, day);

    /// <summary>
    /// Verifies Orthodox (Julian) Easter Sunday for every year in the known-answer table.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected month.</param>
    /// <param name="day">The expected day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(JulianRows), DynamicDataSourceType.Method)]
    public void OrthodoxEaster_MatchesKnownAnswer(int year, int month, int day) =>
        AssertEaster("orthodox-easter", year, month, day);
}
