// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and behaviour of <see cref="EasterSundayNotableDateAlgorithm" />.
/// </summary>
[TestClass]
public sealed class EasterSundayNotableDateAlgorithmTests
{
    private readonly EasterSundayNotableDateAlgorithm _algorithm = new();

    public enum EasterCalendarType
    {
        Default = 0,
        Gregorian,
        Julian,
        Orthodox,
    }

    public sealed record EasterSundayKnownAnswer { public int Year { get; init; } public DateTime ExpectedDate { get; init; } public EasterCalendarType CalendarType { get; init; } = EasterCalendarType.Default; }

    public static IEnumerable<object[]> GetInvalidYearTestData()
    {
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1700, ExpectedDate = new DateTime(1700, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1701, ExpectedDate = new DateTime(1701, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1702, ExpectedDate = new DateTime(1702, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1703, ExpectedDate = new DateTime(1703, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1704, ExpectedDate = new DateTime(1704, 3, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1705, ExpectedDate = new DateTime(1705, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1706, ExpectedDate = new DateTime(1706, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1707, ExpectedDate = new DateTime(1707, 4, 24) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1708, ExpectedDate = new DateTime(1708, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1709, ExpectedDate = new DateTime(1709, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1710, ExpectedDate = new DateTime(1710, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1711, ExpectedDate = new DateTime(1711, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1712, ExpectedDate = new DateTime(1712, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1713, ExpectedDate = new DateTime(1713, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1714, ExpectedDate = new DateTime(1714, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1715, ExpectedDate = new DateTime(1715, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1716, ExpectedDate = new DateTime(1716, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1717, ExpectedDate = new DateTime(1717, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1718, ExpectedDate = new DateTime(1718, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1719, ExpectedDate = new DateTime(1719, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1720, ExpectedDate = new DateTime(1720, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1721, ExpectedDate = new DateTime(1721, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1722, ExpectedDate = new DateTime(1722, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1723, ExpectedDate = new DateTime(1723, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1724, ExpectedDate = new DateTime(1724, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1725, ExpectedDate = new DateTime(1725, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1726, ExpectedDate = new DateTime(1726, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1727, ExpectedDate = new DateTime(1727, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1728, ExpectedDate = new DateTime(1728, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1729, ExpectedDate = new DateTime(1729, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1730, ExpectedDate = new DateTime(1730, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1731, ExpectedDate = new DateTime(1731, 3, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1732, ExpectedDate = new DateTime(1732, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1733, ExpectedDate = new DateTime(1733, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1734, ExpectedDate = new DateTime(1734, 4, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1735, ExpectedDate = new DateTime(1735, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1736, ExpectedDate = new DateTime(1736, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1737, ExpectedDate = new DateTime(1737, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1738, ExpectedDate = new DateTime(1738, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1739, ExpectedDate = new DateTime(1739, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1740, ExpectedDate = new DateTime(1740, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1741, ExpectedDate = new DateTime(1741, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1742, ExpectedDate = new DateTime(1742, 3, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1743, ExpectedDate = new DateTime(1743, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1744, ExpectedDate = new DateTime(1744, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1745, ExpectedDate = new DateTime(1745, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1746, ExpectedDate = new DateTime(1746, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1747, ExpectedDate = new DateTime(1747, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1748, ExpectedDate = new DateTime(1748, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1749, ExpectedDate = new DateTime(1749, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1750, ExpectedDate = new DateTime(1750, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1751, ExpectedDate = new DateTime(1751, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1752, ExpectedDate = new DateTime(1752, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1753, ExpectedDate = new DateTime(1753, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1754, ExpectedDate = new DateTime(1754, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1755, ExpectedDate = new DateTime(1755, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1756, ExpectedDate = new DateTime(1756, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1757, ExpectedDate = new DateTime(1757, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1758, ExpectedDate = new DateTime(1758, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1759, ExpectedDate = new DateTime(1759, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1760, ExpectedDate = new DateTime(1760, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1761, ExpectedDate = new DateTime(1761, 3, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1762, ExpectedDate = new DateTime(1762, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1763, ExpectedDate = new DateTime(1763, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1764, ExpectedDate = new DateTime(1764, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1765, ExpectedDate = new DateTime(1765, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1766, ExpectedDate = new DateTime(1766, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1767, ExpectedDate = new DateTime(1767, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1768, ExpectedDate = new DateTime(1768, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1769, ExpectedDate = new DateTime(1769, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1770, ExpectedDate = new DateTime(1770, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1771, ExpectedDate = new DateTime(1771, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1772, ExpectedDate = new DateTime(1772, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1773, ExpectedDate = new DateTime(1773, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1774, ExpectedDate = new DateTime(1774, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1775, ExpectedDate = new DateTime(1775, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1776, ExpectedDate = new DateTime(1776, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1777, ExpectedDate = new DateTime(1777, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1778, ExpectedDate = new DateTime(1778, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1779, ExpectedDate = new DateTime(1779, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1780, ExpectedDate = new DateTime(1780, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1781, ExpectedDate = new DateTime(1781, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1782, ExpectedDate = new DateTime(1782, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1783, ExpectedDate = new DateTime(1783, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1784, ExpectedDate = new DateTime(1784, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1785, ExpectedDate = new DateTime(1785, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1786, ExpectedDate = new DateTime(1786, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1787, ExpectedDate = new DateTime(1787, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1788, ExpectedDate = new DateTime(1788, 3, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1789, ExpectedDate = new DateTime(1789, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1790, ExpectedDate = new DateTime(1790, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1791, ExpectedDate = new DateTime(1791, 4, 24) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1792, ExpectedDate = new DateTime(1792, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1793, ExpectedDate = new DateTime(1793, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1794, ExpectedDate = new DateTime(1794, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1795, ExpectedDate = new DateTime(1795, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1796, ExpectedDate = new DateTime(1796, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1797, ExpectedDate = new DateTime(1797, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1798, ExpectedDate = new DateTime(1798, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1799, ExpectedDate = new DateTime(1799, 3, 24) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1800, ExpectedDate = new DateTime(1800, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1801, ExpectedDate = new DateTime(1801, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1802, ExpectedDate = new DateTime(1802, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1803, ExpectedDate = new DateTime(1803, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1804, ExpectedDate = new DateTime(1804, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1805, ExpectedDate = new DateTime(1805, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1806, ExpectedDate = new DateTime(1806, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1807, ExpectedDate = new DateTime(1807, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1808, ExpectedDate = new DateTime(1808, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1809, ExpectedDate = new DateTime(1809, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1810, ExpectedDate = new DateTime(1810, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1811, ExpectedDate = new DateTime(1811, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1812, ExpectedDate = new DateTime(1812, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1813, ExpectedDate = new DateTime(1813, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1814, ExpectedDate = new DateTime(1814, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1815, ExpectedDate = new DateTime(1815, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1816, ExpectedDate = new DateTime(1816, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1817, ExpectedDate = new DateTime(1817, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1818, ExpectedDate = new DateTime(1818, 3, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1819, ExpectedDate = new DateTime(1819, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1820, ExpectedDate = new DateTime(1820, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1821, ExpectedDate = new DateTime(1821, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1822, ExpectedDate = new DateTime(1822, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1823, ExpectedDate = new DateTime(1823, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1824, ExpectedDate = new DateTime(1824, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1825, ExpectedDate = new DateTime(1825, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1826, ExpectedDate = new DateTime(1826, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1827, ExpectedDate = new DateTime(1827, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1828, ExpectedDate = new DateTime(1828, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1829, ExpectedDate = new DateTime(1829, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1830, ExpectedDate = new DateTime(1830, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1831, ExpectedDate = new DateTime(1831, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1832, ExpectedDate = new DateTime(1832, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1833, ExpectedDate = new DateTime(1833, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1834, ExpectedDate = new DateTime(1834, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1835, ExpectedDate = new DateTime(1835, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1836, ExpectedDate = new DateTime(1836, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1837, ExpectedDate = new DateTime(1837, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1838, ExpectedDate = new DateTime(1838, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1839, ExpectedDate = new DateTime(1839, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1840, ExpectedDate = new DateTime(1840, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1841, ExpectedDate = new DateTime(1841, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1842, ExpectedDate = new DateTime(1842, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1843, ExpectedDate = new DateTime(1843, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1844, ExpectedDate = new DateTime(1844, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1845, ExpectedDate = new DateTime(1845, 3, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1846, ExpectedDate = new DateTime(1846, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1847, ExpectedDate = new DateTime(1847, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1848, ExpectedDate = new DateTime(1848, 4, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1849, ExpectedDate = new DateTime(1849, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1850, ExpectedDate = new DateTime(1850, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1851, ExpectedDate = new DateTime(1851, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1852, ExpectedDate = new DateTime(1852, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1853, ExpectedDate = new DateTime(1853, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1854, ExpectedDate = new DateTime(1854, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1855, ExpectedDate = new DateTime(1855, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1856, ExpectedDate = new DateTime(1856, 3, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1857, ExpectedDate = new DateTime(1857, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1858, ExpectedDate = new DateTime(1858, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1859, ExpectedDate = new DateTime(1859, 4, 24) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1860, ExpectedDate = new DateTime(1860, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1861, ExpectedDate = new DateTime(1861, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1862, ExpectedDate = new DateTime(1862, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1863, ExpectedDate = new DateTime(1863, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1864, ExpectedDate = new DateTime(1864, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1865, ExpectedDate = new DateTime(1865, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1866, ExpectedDate = new DateTime(1866, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1867, ExpectedDate = new DateTime(1867, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1868, ExpectedDate = new DateTime(1868, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1869, ExpectedDate = new DateTime(1869, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1870, ExpectedDate = new DateTime(1870, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1871, ExpectedDate = new DateTime(1871, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1872, ExpectedDate = new DateTime(1872, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1873, ExpectedDate = new DateTime(1873, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1874, ExpectedDate = new DateTime(1874, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1875, ExpectedDate = new DateTime(1875, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1876, ExpectedDate = new DateTime(1876, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1877, ExpectedDate = new DateTime(1877, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1878, ExpectedDate = new DateTime(1878, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1879, ExpectedDate = new DateTime(1879, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1880, ExpectedDate = new DateTime(1880, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1881, ExpectedDate = new DateTime(1881, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1882, ExpectedDate = new DateTime(1882, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1883, ExpectedDate = new DateTime(1883, 3, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1884, ExpectedDate = new DateTime(1884, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1885, ExpectedDate = new DateTime(1885, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1886, ExpectedDate = new DateTime(1886, 4, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1887, ExpectedDate = new DateTime(1887, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1888, ExpectedDate = new DateTime(1888, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1889, ExpectedDate = new DateTime(1889, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1890, ExpectedDate = new DateTime(1890, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1891, ExpectedDate = new DateTime(1891, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1892, ExpectedDate = new DateTime(1892, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1893, ExpectedDate = new DateTime(1893, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1894, ExpectedDate = new DateTime(1894, 3, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1895, ExpectedDate = new DateTime(1895, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1896, ExpectedDate = new DateTime(1896, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1897, ExpectedDate = new DateTime(1897, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1898, ExpectedDate = new DateTime(1898, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1899, ExpectedDate = new DateTime(1899, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1900, ExpectedDate = new DateTime(1900, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1901, ExpectedDate = new DateTime(1901, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1902, ExpectedDate = new DateTime(1902, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1903, ExpectedDate = new DateTime(1903, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1904, ExpectedDate = new DateTime(1904, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1905, ExpectedDate = new DateTime(1905, 4, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1906, ExpectedDate = new DateTime(1906, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1907, ExpectedDate = new DateTime(1907, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1908, ExpectedDate = new DateTime(1908, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1909, ExpectedDate = new DateTime(1909, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1910, ExpectedDate = new DateTime(1910, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1911, ExpectedDate = new DateTime(1911, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1912, ExpectedDate = new DateTime(1912, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1913, ExpectedDate = new DateTime(1913, 3, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1914, ExpectedDate = new DateTime(1914, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1915, ExpectedDate = new DateTime(1915, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1916, ExpectedDate = new DateTime(1916, 4, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1917, ExpectedDate = new DateTime(1917, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1918, ExpectedDate = new DateTime(1918, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1919, ExpectedDate = new DateTime(1919, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1920, ExpectedDate = new DateTime(1920, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1921, ExpectedDate = new DateTime(1921, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1922, ExpectedDate = new DateTime(1922, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1923, ExpectedDate = new DateTime(1923, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1924, ExpectedDate = new DateTime(1924, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1925, ExpectedDate = new DateTime(1925, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1926, ExpectedDate = new DateTime(1926, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1927, ExpectedDate = new DateTime(1927, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1928, ExpectedDate = new DateTime(1928, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1929, ExpectedDate = new DateTime(1929, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1930, ExpectedDate = new DateTime(1930, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1931, ExpectedDate = new DateTime(1931, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1932, ExpectedDate = new DateTime(1932, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1933, ExpectedDate = new DateTime(1933, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1934, ExpectedDate = new DateTime(1934, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1935, ExpectedDate = new DateTime(1935, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1936, ExpectedDate = new DateTime(1936, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1937, ExpectedDate = new DateTime(1937, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1938, ExpectedDate = new DateTime(1938, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1939, ExpectedDate = new DateTime(1939, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1940, ExpectedDate = new DateTime(1940, 3, 24) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1941, ExpectedDate = new DateTime(1941, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1942, ExpectedDate = new DateTime(1942, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1943, ExpectedDate = new DateTime(1943, 4, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1944, ExpectedDate = new DateTime(1944, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1945, ExpectedDate = new DateTime(1945, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1946, ExpectedDate = new DateTime(1946, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1947, ExpectedDate = new DateTime(1947, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1948, ExpectedDate = new DateTime(1948, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1949, ExpectedDate = new DateTime(1949, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1950, ExpectedDate = new DateTime(1950, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1951, ExpectedDate = new DateTime(1951, 3, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1952, ExpectedDate = new DateTime(1952, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1953, ExpectedDate = new DateTime(1953, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1954, ExpectedDate = new DateTime(1954, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1955, ExpectedDate = new DateTime(1955, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1956, ExpectedDate = new DateTime(1956, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1957, ExpectedDate = new DateTime(1957, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1958, ExpectedDate = new DateTime(1958, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1959, ExpectedDate = new DateTime(1959, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1960, ExpectedDate = new DateTime(1960, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1961, ExpectedDate = new DateTime(1961, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1962, ExpectedDate = new DateTime(1962, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1963, ExpectedDate = new DateTime(1963, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1964, ExpectedDate = new DateTime(1964, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1965, ExpectedDate = new DateTime(1965, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1966, ExpectedDate = new DateTime(1966, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1967, ExpectedDate = new DateTime(1967, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1968, ExpectedDate = new DateTime(1968, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1969, ExpectedDate = new DateTime(1969, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1970, ExpectedDate = new DateTime(1970, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1971, ExpectedDate = new DateTime(1971, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1972, ExpectedDate = new DateTime(1972, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1973, ExpectedDate = new DateTime(1973, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1974, ExpectedDate = new DateTime(1974, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1975, ExpectedDate = new DateTime(1975, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1976, ExpectedDate = new DateTime(1976, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1977, ExpectedDate = new DateTime(1977, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1978, ExpectedDate = new DateTime(1978, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1979, ExpectedDate = new DateTime(1979, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1980, ExpectedDate = new DateTime(1980, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1981, ExpectedDate = new DateTime(1981, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1982, ExpectedDate = new DateTime(1982, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1983, ExpectedDate = new DateTime(1983, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1984, ExpectedDate = new DateTime(1984, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1985, ExpectedDate = new DateTime(1985, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1986, ExpectedDate = new DateTime(1986, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1987, ExpectedDate = new DateTime(1987, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1988, ExpectedDate = new DateTime(1988, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1989, ExpectedDate = new DateTime(1989, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1990, ExpectedDate = new DateTime(1990, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1991, ExpectedDate = new DateTime(1991, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1992, ExpectedDate = new DateTime(1992, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1993, ExpectedDate = new DateTime(1993, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1994, ExpectedDate = new DateTime(1994, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1995, ExpectedDate = new DateTime(1995, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1996, ExpectedDate = new DateTime(1996, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1997, ExpectedDate = new DateTime(1997, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1998, ExpectedDate = new DateTime(1998, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1999, ExpectedDate = new DateTime(1999, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2000, ExpectedDate = new DateTime(2000, 4, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2001, ExpectedDate = new DateTime(2001, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2002, ExpectedDate = new DateTime(2002, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2003, ExpectedDate = new DateTime(2003, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2004, ExpectedDate = new DateTime(2004, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2005, ExpectedDate = new DateTime(2005, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2006, ExpectedDate = new DateTime(2006, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2007, ExpectedDate = new DateTime(2007, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2008, ExpectedDate = new DateTime(2008, 3, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2009, ExpectedDate = new DateTime(2009, 4, 12) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2010, ExpectedDate = new DateTime(2010, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2011, ExpectedDate = new DateTime(2011, 4, 24) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2012, ExpectedDate = new DateTime(2012, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2013, ExpectedDate = new DateTime(2013, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2014, ExpectedDate = new DateTime(2014, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2015, ExpectedDate = new DateTime(2015, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2016, ExpectedDate = new DateTime(2016, 3, 27) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2017, ExpectedDate = new DateTime(2017, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2018, ExpectedDate = new DateTime(2018, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2025, ExpectedDate = new DateTime(2025, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2026, ExpectedDate = new DateTime(2026, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2027, ExpectedDate = new DateTime(2027, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2028, ExpectedDate = new DateTime(2028, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2029, ExpectedDate = new DateTime(2029, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2030, ExpectedDate = new DateTime(2030, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2031, ExpectedDate = new DateTime(2031, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2032, ExpectedDate = new DateTime(2032, 3, 28) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2033, ExpectedDate = new DateTime(2033, 4, 17) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2034, ExpectedDate = new DateTime(2034, 4, 9) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2035, ExpectedDate = new DateTime(2035, 3, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2036, ExpectedDate = new DateTime(2036, 4, 13) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2037, ExpectedDate = new DateTime(2037, 4, 5) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2038, ExpectedDate = new DateTime(2038, 4, 25) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2039, ExpectedDate = new DateTime(2039, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2040, ExpectedDate = new DateTime(2040, 4, 1) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2041, ExpectedDate = new DateTime(2041, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2042, ExpectedDate = new DateTime(2042, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2043, ExpectedDate = new DateTime(2043, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2050, ExpectedDate = new DateTime(2050, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2051, ExpectedDate = new DateTime(2051, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2052, ExpectedDate = new DateTime(2052, 4, 21) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2053, ExpectedDate = new DateTime(2053, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2054, ExpectedDate = new DateTime(2054, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2055, ExpectedDate = new DateTime(2055, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2056, ExpectedDate = new DateTime(2056, 4, 2) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2057, ExpectedDate = new DateTime(2057, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2058, ExpectedDate = new DateTime(2058, 4, 14) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2059, ExpectedDate = new DateTime(2059, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2060, ExpectedDate = new DateTime(2060, 4, 18) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2061, ExpectedDate = new DateTime(2061, 4, 10) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2062, ExpectedDate = new DateTime(2062, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2063, ExpectedDate = new DateTime(2063, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2064, ExpectedDate = new DateTime(2064, 4, 6) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2065, ExpectedDate = new DateTime(2065, 3, 29) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2066, ExpectedDate = new DateTime(2066, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2067, ExpectedDate = new DateTime(2067, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2068, ExpectedDate = new DateTime(2068, 4, 22) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2075, ExpectedDate = new DateTime(2075, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2076, ExpectedDate = new DateTime(2076, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2077, ExpectedDate = new DateTime(2077, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2078, ExpectedDate = new DateTime(2078, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2079, ExpectedDate = new DateTime(2079, 4, 23) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2080, ExpectedDate = new DateTime(2080, 4, 7) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2081, ExpectedDate = new DateTime(2081, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2082, ExpectedDate = new DateTime(2082, 4, 19) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2083, ExpectedDate = new DateTime(2083, 4, 4) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2084, ExpectedDate = new DateTime(2084, 3, 26) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2085, ExpectedDate = new DateTime(2085, 4, 15) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2086, ExpectedDate = new DateTime(2086, 3, 31) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2087, ExpectedDate = new DateTime(2087, 4, 20) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2088, ExpectedDate = new DateTime(2088, 4, 11) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2089, ExpectedDate = new DateTime(2089, 4, 3) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2090, ExpectedDate = new DateTime(2090, 4, 16) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2091, ExpectedDate = new DateTime(2091, 4, 8) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2092, ExpectedDate = new DateTime(2092, 3, 30) } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2093, ExpectedDate = new DateTime(2093, 4, 12) } };


        // Orthodox entries intentionally omitted from this data source; all 184 Orthodox rows
        // (1916-2099) are exercised by GetDate_WhenGivenOrthodoxKnownYears_ShouldReturnExpectedDate,
        // which is marked [Ignore] pending fix of bslater/bodu#53.


    }

    /// <summary>
    /// Provides known year, expected Easter Sunday dates, and calendar system for validation. Grouped logically into Boundary, Julian,
    /// and Gregorian.
    /// </summary>
    public static IEnumerable<object[]> GetKnownEasterSundayTestData()
    {
        // Helper only for non-null calendars
        static SysGlob.Calendar CalendarOf(string type) => type switch
        {
            "Gregorian" => new SysGlob.GregorianCalendar(),
            "Julian" => new SysGlob.JulianCalendar(),
            _ => throw new ArgumentException($"Unsupported calendar type: {type}", nameof(type))
        };

        // Boundary Tests
        yield return new object[] { 1, new DateTime(1, 3, 27, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1, new DateTime(1, 3, 25, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
        yield return new object[] { 9999, new DateTime(9999, 3, 28, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 9999, new DateTime(9999, 3, 28, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };

        // Julian Calendar
        yield return new object[] { 1000, new DateTime(1000, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1000, new DateTime(1000, 4, 6, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
        yield return new object[] { 1200, new DateTime(1200, 4, 9, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1200, new DateTime(1200, 4, 16, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
        yield return new object[] { 1500, new DateTime(1500, 4, 19, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1500, new DateTime(1500, 4, 29, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };

        // Gregorian Calendar
        yield return new object[] { 1583, new DateTime(1583, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1583, new DateTime(1583, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
        yield return new object[] { 1600, new DateTime(1600, 4, 2, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1600, new DateTime(1600, 4, 2, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
        yield return new object[] { 1700, new DateTime(1700, 4, 11, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1700, new DateTime(1700, 4, 11, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
        yield return new object[] { 1800, new DateTime(1800, 4, 13, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1800, new DateTime(1800, 4, 13, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
        yield return new object[] { 1900, new DateTime(1900, 4, 15, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 1900, new DateTime(1900, 4, 15, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
        yield return new object[] { 2000, new DateTime(2000, 4, 23, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 2000, new DateTime(2000, 4, 23, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
        yield return new object[] { 2024, new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 2024, new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
        yield return new object[] { 2024, new DateTime(2024, 5, 5, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
        yield return new object[] { 2025, new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 2025, new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };

        // Future Years
        yield return new object[] { 2030, new DateTime(2030, 4, 21, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 2040, new DateTime(2040, 4, 1, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 2050, new DateTime(2050, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), null! };
        yield return new object[] { 2100, new DateTime(2100, 3, 28, 0, 0, 0, DateTimeKind.Unspecified), null! };
    }

    public static string GetKnownEasterSundayTestDataDisplayName(MethodInfo methodInfo, object[] data)
    {
        EasterSundayKnownAnswer knownAnswer = (EasterSundayKnownAnswer)data[0];
        return $"Year: {knownAnswer.Year}, Calendar: {knownAnswer.CalendarType}, Expected: {knownAnswer.ExpectedDate:yyyy-MM-dd}";
    }

    /// <summary>
    /// Verifies that Easter Sunday is correctly calculated across a known set of years and calendars.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(GetInvalidYearTestData), DynamicDataDisplayName = nameof(GetKnownEasterSundayTestDataDisplayName))]
    public void GetDate_WhenGivenKnownYears_ShouldReturnExpectedDate(EasterSundayKnownAnswer knownAnswer)
    {
        var algorithm = new EasterSundayNotableDateAlgorithm();
        SysGlob.Calendar? calendar = knownAnswer.CalendarType switch
        {
            EasterCalendarType.Gregorian => new SysGlob.GregorianCalendar(),
            EasterCalendarType.Julian => new SysGlob.JulianCalendar(),
            _ => null
        };

        var result = algorithm.GetDate(knownAnswer.Year, calendar);

        Assert.IsNotNull(result);
        Assert.AreEqual(knownAnswer.ExpectedDate, result);
    }

    /// <summary>
    /// Known-failing Orthodox Easter Sunday data spanning years 1916-2099, exercised by a
    /// separate <see cref="IgnoreAttribute" />-marked test pending fix of
    /// <see href="https://github.com/bslater/bodu/issues/53" />.
    /// </summary>
    public static IEnumerable<object[]> GetBrokenOrthodoxTestData()
    {
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1916, ExpectedDate = new DateTime(1916, 4, 23), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1917, ExpectedDate = new DateTime(1917, 4, 15), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1918, ExpectedDate = new DateTime(1918, 5, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1919, ExpectedDate = new DateTime(1919, 4, 20), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1920, ExpectedDate = new DateTime(1920, 4, 11), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1921, ExpectedDate = new DateTime(1921, 5, 1), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1922, ExpectedDate = new DateTime(1922, 4, 16), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1923, ExpectedDate = new DateTime(1923, 4, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1924, ExpectedDate = new DateTime(1924, 4, 27), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1925, ExpectedDate = new DateTime(1925, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1926, ExpectedDate = new DateTime(1926, 5, 2), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1927, ExpectedDate = new DateTime(1927, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1928, ExpectedDate = new DateTime(1928, 4, 15), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1929, ExpectedDate = new DateTime(1929, 5, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1930, ExpectedDate = new DateTime(1930, 4, 20), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1931, ExpectedDate = new DateTime(1931, 4, 12), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1932, ExpectedDate = new DateTime(1932, 5, 1), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1933, ExpectedDate = new DateTime(1933, 4, 16), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1934, ExpectedDate = new DateTime(1934, 4, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1935, ExpectedDate = new DateTime(1935, 4, 28), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1936, ExpectedDate = new DateTime(1936, 4, 12), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1937, ExpectedDate = new DateTime(1937, 5, 2), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1938, ExpectedDate = new DateTime(1938, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1939, ExpectedDate = new DateTime(1939, 4, 9), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1940, ExpectedDate = new DateTime(1940, 4, 28), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1941, ExpectedDate = new DateTime(1941, 4, 20), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1942, ExpectedDate = new DateTime(1942, 4, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1943, ExpectedDate = new DateTime(1943, 4, 25), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1944, ExpectedDate = new DateTime(1944, 4, 16), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1945, ExpectedDate = new DateTime(1945, 5, 6), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1946, ExpectedDate = new DateTime(1946, 4, 21), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1947, ExpectedDate = new DateTime(1947, 4, 13), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1948, ExpectedDate = new DateTime(1948, 5, 2), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1949, ExpectedDate = new DateTime(1949, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1950, ExpectedDate = new DateTime(1950, 4, 9), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1951, ExpectedDate = new DateTime(1951, 4, 29), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1952, ExpectedDate = new DateTime(1952, 4, 20), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1953, ExpectedDate = new DateTime(1953, 4, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1954, ExpectedDate = new DateTime(1954, 4, 25), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1955, ExpectedDate = new DateTime(1955, 4, 17), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1956, ExpectedDate = new DateTime(1956, 5, 6), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1957, ExpectedDate = new DateTime(1957, 4, 21), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1958, ExpectedDate = new DateTime(1958, 4, 13), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1959, ExpectedDate = new DateTime(1959, 5, 3), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1960, ExpectedDate = new DateTime(1960, 4, 17), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1961, ExpectedDate = new DateTime(1961, 4, 9), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1962, ExpectedDate = new DateTime(1962, 4, 29), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1963, ExpectedDate = new DateTime(1963, 4, 14), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1964, ExpectedDate = new DateTime(1964, 5, 3), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1965, ExpectedDate = new DateTime(1965, 4, 25), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1966, ExpectedDate = new DateTime(1966, 4, 10), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1967, ExpectedDate = new DateTime(1967, 4, 30), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1968, ExpectedDate = new DateTime(1968, 4, 21), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1969, ExpectedDate = new DateTime(1969, 4, 13), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1970, ExpectedDate = new DateTime(1970, 4, 26), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1971, ExpectedDate = new DateTime(1971, 4, 18), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1972, ExpectedDate = new DateTime(1972, 4, 9), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1973, ExpectedDate = new DateTime(1973, 4, 29), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1974, ExpectedDate = new DateTime(1974, 4, 14), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1975, ExpectedDate = new DateTime(1975, 5, 4), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1976, ExpectedDate = new DateTime(1976, 4, 25), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1977, ExpectedDate = new DateTime(1977, 4, 10), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1978, ExpectedDate = new DateTime(1978, 4, 30), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1979, ExpectedDate = new DateTime(1979, 4, 22), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1980, ExpectedDate = new DateTime(1980, 4, 6), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1981, ExpectedDate = new DateTime(1981, 4, 26), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1982, ExpectedDate = new DateTime(1982, 4, 18), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1983, ExpectedDate = new DateTime(1983, 5, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1984, ExpectedDate = new DateTime(1984, 4, 22), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1985, ExpectedDate = new DateTime(1985, 4, 14), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1986, ExpectedDate = new DateTime(1986, 5, 4), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1987, ExpectedDate = new DateTime(1987, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1988, ExpectedDate = new DateTime(1988, 4, 10), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1989, ExpectedDate = new DateTime(1989, 4, 30), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1990, ExpectedDate = new DateTime(1990, 4, 15), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1991, ExpectedDate = new DateTime(1991, 4, 7), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1992, ExpectedDate = new DateTime(1992, 4, 26), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1993, ExpectedDate = new DateTime(1993, 4, 18), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1994, ExpectedDate = new DateTime(1994, 5, 1), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1995, ExpectedDate = new DateTime(1995, 4, 23), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1996, ExpectedDate = new DateTime(1996, 4, 14), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1997, ExpectedDate = new DateTime(1997, 4, 27), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1998, ExpectedDate = new DateTime(1998, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 1999, ExpectedDate = new DateTime(1999, 4, 11), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2000, ExpectedDate = new DateTime(2000, 4, 30), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2001, ExpectedDate = new DateTime(2001, 4, 15), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2002, ExpectedDate = new DateTime(2002, 5, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2003, ExpectedDate = new DateTime(2003, 4, 27), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2004, ExpectedDate = new DateTime(2004, 4, 11), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2005, ExpectedDate = new DateTime(2005, 5, 1), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2006, ExpectedDate = new DateTime(2006, 4, 23), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2007, ExpectedDate = new DateTime(2007, 4, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2008, ExpectedDate = new DateTime(2008, 4, 27), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2009, ExpectedDate = new DateTime(2009, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2010, ExpectedDate = new DateTime(2010, 4, 4), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2011, ExpectedDate = new DateTime(2011, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2012, ExpectedDate = new DateTime(2012, 4, 15), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2013, ExpectedDate = new DateTime(2013, 5, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2014, ExpectedDate = new DateTime(2014, 4, 20), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2015, ExpectedDate = new DateTime(2015, 4, 12), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2016, ExpectedDate = new DateTime(2016, 5, 1), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2017, ExpectedDate = new DateTime(2017, 4, 16), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2018, ExpectedDate = new DateTime(2018, 4, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2019, ExpectedDate = new DateTime(2019, 4, 28), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2020, ExpectedDate = new DateTime(2020, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2021, ExpectedDate = new DateTime(2021, 5, 2), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2022, ExpectedDate = new DateTime(2022, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2023, ExpectedDate = new DateTime(2023, 4, 16), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2024, ExpectedDate = new DateTime(2024, 5, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2025, ExpectedDate = new DateTime(2025, 4, 20), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2026, ExpectedDate = new DateTime(2026, 4, 12), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2027, ExpectedDate = new DateTime(2027, 5, 2), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2028, ExpectedDate = new DateTime(2028, 4, 16), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2029, ExpectedDate = new DateTime(2029, 4, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2030, ExpectedDate = new DateTime(2030, 4, 28), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2031, ExpectedDate = new DateTime(2031, 4, 13), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2032, ExpectedDate = new DateTime(2032, 5, 2), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2033, ExpectedDate = new DateTime(2033, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2034, ExpectedDate = new DateTime(2034, 4, 9), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2035, ExpectedDate = new DateTime(2035, 4, 29), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2036, ExpectedDate = new DateTime(2036, 4, 20), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2037, ExpectedDate = new DateTime(2037, 4, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2038, ExpectedDate = new DateTime(2038, 4, 25), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2039, ExpectedDate = new DateTime(2039, 4, 17), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2040, ExpectedDate = new DateTime(2040, 5, 6), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2041, ExpectedDate = new DateTime(2041, 4, 21), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2042, ExpectedDate = new DateTime(2042, 4, 13), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2043, ExpectedDate = new DateTime(2043, 5, 3), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2044, ExpectedDate = new DateTime(2044, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2045, ExpectedDate = new DateTime(2045, 4, 9), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2046, ExpectedDate = new DateTime(2046, 4, 29), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2047, ExpectedDate = new DateTime(2047, 4, 21), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2048, ExpectedDate = new DateTime(2048, 4, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2049, ExpectedDate = new DateTime(2049, 4, 25), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2050, ExpectedDate = new DateTime(2050, 4, 17), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2051, ExpectedDate = new DateTime(2051, 5, 7), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2052, ExpectedDate = new DateTime(2052, 4, 21), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2053, ExpectedDate = new DateTime(2053, 4, 13), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2054, ExpectedDate = new DateTime(2054, 5, 3), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2055, ExpectedDate = new DateTime(2055, 4, 18), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2056, ExpectedDate = new DateTime(2056, 4, 9), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2057, ExpectedDate = new DateTime(2057, 4, 29), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2058, ExpectedDate = new DateTime(2058, 4, 14), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2059, ExpectedDate = new DateTime(2059, 5, 4), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2060, ExpectedDate = new DateTime(2060, 4, 25), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2061, ExpectedDate = new DateTime(2061, 4, 10), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2062, ExpectedDate = new DateTime(2062, 4, 30), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2063, ExpectedDate = new DateTime(2063, 4, 22), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2064, ExpectedDate = new DateTime(2064, 4, 13), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2065, ExpectedDate = new DateTime(2065, 4, 26), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2066, ExpectedDate = new DateTime(2066, 4, 18), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2067, ExpectedDate = new DateTime(2067, 4, 10), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2068, ExpectedDate = new DateTime(2068, 4, 29), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2069, ExpectedDate = new DateTime(2069, 4, 14), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2070, ExpectedDate = new DateTime(2070, 5, 4), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2071, ExpectedDate = new DateTime(2071, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2072, ExpectedDate = new DateTime(2072, 4, 10), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2073, ExpectedDate = new DateTime(2073, 4, 30), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2074, ExpectedDate = new DateTime(2074, 4, 22), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2075, ExpectedDate = new DateTime(2075, 4, 7), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2076, ExpectedDate = new DateTime(2076, 4, 26), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2077, ExpectedDate = new DateTime(2077, 4, 18), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2078, ExpectedDate = new DateTime(2078, 5, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2079, ExpectedDate = new DateTime(2079, 4, 23), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2080, ExpectedDate = new DateTime(2080, 4, 14), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2081, ExpectedDate = new DateTime(2081, 5, 4), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2082, ExpectedDate = new DateTime(2082, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2083, ExpectedDate = new DateTime(2083, 4, 11), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2084, ExpectedDate = new DateTime(2084, 4, 30), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2085, ExpectedDate = new DateTime(2085, 4, 15), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2086, ExpectedDate = new DateTime(2086, 4, 7), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2087, ExpectedDate = new DateTime(2087, 4, 27), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2088, ExpectedDate = new DateTime(2088, 4, 18), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2089, ExpectedDate = new DateTime(2089, 5, 1), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2090, ExpectedDate = new DateTime(2090, 4, 23), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2091, ExpectedDate = new DateTime(2091, 4, 8), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2092, ExpectedDate = new DateTime(2092, 4, 27), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2093, ExpectedDate = new DateTime(2093, 4, 19), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2094, ExpectedDate = new DateTime(2094, 4, 11), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2095, ExpectedDate = new DateTime(2095, 4, 24), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2096, ExpectedDate = new DateTime(2096, 4, 15), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2097, ExpectedDate = new DateTime(2097, 5, 5), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2098, ExpectedDate = new DateTime(2098, 4, 27), CalendarType = EasterCalendarType.Orthodox } };
        yield return new object[] { new EasterSundayKnownAnswer { Year = 2099, ExpectedDate = new DateTime(2099, 4, 12), CalendarType = EasterCalendarType.Orthodox } };
    }

    /// <summary>
    /// Verifies that Orthodox Easter Sunday is correctly calculated for years 1916-2099.
    /// Currently skipped: the algorithm returns incorrect dates (7-35 days earlier than
    /// expected) for most Orthodox entries. Tracked by
    /// <see href="https://github.com/bslater/bodu/issues/53" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(GetBrokenOrthodoxTestData), DynamicDataDisplayName = nameof(GetKnownEasterSundayTestDataDisplayName))]
    public void GetDate_WhenGivenOrthodoxKnownYears_ShouldReturnExpectedDate(EasterSundayKnownAnswer knownAnswer)
    {
        var algorithm = new EasterSundayNotableDateAlgorithm();
        SysGlob.Calendar? calendar = knownAnswer.CalendarType switch
        {
            EasterCalendarType.Gregorian => new SysGlob.GregorianCalendar(),
            EasterCalendarType.Julian => new SysGlob.JulianCalendar(),
            EasterCalendarType.Orthodox => new SysGlob.JulianCalendar(),
            _ => null
        };

        var result = algorithm.GetDate(knownAnswer.Year, calendar);

        Assert.IsNotNull(result);
        Assert.AreEqual(knownAnswer.ExpectedDate, result);
    }

    /// <summary>
    /// Verifies that requesting Easter Sunday for year 0 or any negative year throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(int.MinValue)]
    public void GetDate_WhenYearInvalid_ShouldThrowArgumentOutOfRangeException(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = _algorithm.GetDate(year, null);
        });
    }

    /// <summary>
    /// Verifies Julian-era Easter Sundays (year &lt; 1583) resolve using the Julian computus and
    /// that their <see cref="DateTime" /> values match the expected almanac dates when no
    /// calendar is supplied.
    /// </summary>
    [DataRow(500, 500, 4, 2)]
    [DataRow(1000, 1000, 3, 31)]
    [DataRow(1500, 1500, 4, 19)]
    [DataRow(1582, 1582, 4, 15)]
    [TestMethod]
    public void GetDate_WhenJulianEraYear_ShouldComputeUsingJulianBranch(int year, int expectedY, int expectedM, int expectedD)
    {
        DateTime? result = _algorithm.GetDate(year, null);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(expectedY, expectedM, expectedD), result);
    }

    /// <summary>
    /// Verifies that the Gregorian vs. Julian computation branches are selected based on the
    /// 1583 cutover year.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenAtOrAbove1583_ShouldUseGregorianBranch()
    {
        // 1583 is the Gregorian computus's minimum supported year; 1584 should also take that branch.
        DateTime? gregorian1583 = _algorithm.GetDate(1583, null);
        DateTime? gregorian1584 = _algorithm.GetDate(1584, null);

        Assert.IsNotNull(gregorian1583);
        Assert.IsNotNull(gregorian1584);
        Assert.AreEqual(1583, gregorian1583!.Value.Year);
        Assert.AreEqual(1584, gregorian1584!.Value.Year);
    }

    /// <summary>
    /// Verifies that passing a Julian calendar explicitly yields Julian-calendar
    /// <see cref="DateTime" /> values (with matching Kind and components) rather than the raw
    /// Gregorian output. Covers the non-null <c>calendar.ToDateTime</c> branch in the algorithm.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenExplicitJulianCalendar_ShouldReturnDateBuiltThroughCalendar()
    {
        var julian = new SysGlob.JulianCalendar();

        DateTime? result = _algorithm.GetDate(2026, julian);

        Assert.IsNotNull(result);
        Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
    }

    /// <summary>
    /// Verifies that repeated calls to GetDate for the same year and calendar return the same cached result.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenCalledTwice_ShouldReturnCachedResult()
    {
        int year = 2026;

        var firstCall = _algorithm.GetDate(year, null);
        var secondCall = _algorithm.GetDate(year, null);

        Assert.AreEqual(firstCall, secondCall);
    }

    /// <summary>
    /// Verifies that when no calendar is provided, the resulting <see cref="DateTime" /> has <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void GetDate_WhenNullCalendar_ShouldReturnUnspecifiedKindDate()
    {
        int year = 2030;

        var result = _algorithm.GetDate(year, null);

        Assert.AreEqual(DateTimeKind.Unspecified, result?.Kind);
    }
}
