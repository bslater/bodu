// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandard.Catalog.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides the catalogue of well-known CRC parameter sets sourced from the published CRC RevEng reference.
    /// </summary>
    public sealed partial class CrcStandard
    {
        /// <summary>
        /// Represents the <c>CRC-3/GSM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC3_GSM = new CrcStandard(
            name: "CRC-3/GSM",
            size: 3,
            polynomial: 0X3UL,
            initialValue: 0X0UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X7UL);

        /// <summary>
        /// Represents the <c>CRC-3/ROHC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC3_ROHC = new CrcStandard(
            name: "CRC-3/ROHC",
            size: 3,
            polynomial: 0X3UL,
            initialValue: 0X7UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0UL);

        /// <summary>
        /// Represents the <c>CRC-4/G-704</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC4_G704 = new CrcStandard(
            name: "CRC-4/G-704",
            size: 4,
            polynomial: 0X3UL,
            initialValue: 0X0UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0UL);

        /// <summary>
        /// Represents the <c>CRC-4/INTERLAKEN</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC4_INTERLAKEN = new CrcStandard(
            name: "CRC-4/INTERLAKEN",
            size: 4,
            polynomial: 0X3UL,
            initialValue: 0XFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFUL);

        /// <summary>
        /// Represents the <c>CRC-5/EPC-C1G2</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC5_EPCC1G2 = new CrcStandard(
            name: "CRC-5/EPC-C1G2",
            size: 5,
            polynomial: 0X09UL,
            initialValue: 0X09UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-5/G-704</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC5_G704 = new CrcStandard(
            name: "CRC-5/G-704",
            size: 5,
            polynomial: 0X15UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-5/USB</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC5_USB = new CrcStandard(
            name: "CRC-5/USB",
            size: 5,
            polynomial: 0X05UL,
            initialValue: 0X1FUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X1FUL);

        /// <summary>
        /// Represents the <c>CRC-6/CDMA2000-A</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC6_CDMA2000A = new CrcStandard(
            name: "CRC-6/CDMA2000-A",
            size: 6,
            polynomial: 0X27UL,
            initialValue: 0X3FUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-6/CDMA2000-B</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC6_CDMA2000B = new CrcStandard(
            name: "CRC-6/CDMA2000-B",
            size: 6,
            polynomial: 0X07UL,
            initialValue: 0X3FUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-6/DARC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC6_DARC = new CrcStandard(
            name: "CRC-6/DARC",
            size: 6,
            polynomial: 0X19UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-6/G-704</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC6_G704 = new CrcStandard(
            name: "CRC-6/G-704",
            size: 6,
            polynomial: 0X03UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-6/GSM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC6_GSM = new CrcStandard(
            name: "CRC-6/GSM",
            size: 6,
            polynomial: 0X2FUL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X3FUL);

        /// <summary>
        /// Represents the <c>CRC-7/MMC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC7_MMC = new CrcStandard(
            name: "CRC-7/MMC",
            size: 7,
            polynomial: 0X09UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-7/ROHC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC7_ROHC = new CrcStandard(
            name: "CRC-7/ROHC",
            size: 7,
            polynomial: 0X4FUL,
            initialValue: 0X7FUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-7/UMTS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC7_UMTS = new CrcStandard(
            name: "CRC-7/UMTS",
            size: 7,
            polynomial: 0X45UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/AUTOSAR</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_AUTOSAR = new CrcStandard(
            name: "CRC-8/AUTOSAR",
            size: 8,
            polynomial: 0X2FUL,
            initialValue: 0XFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFUL);

        /// <summary>
        /// Represents the <c>CRC-8/CDMA2000</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_CDMA2000 = new CrcStandard(
            name: "CRC-8/CDMA2000",
            size: 8,
            polynomial: 0X9BUL,
            initialValue: 0XFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/DARC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_DARC = new CrcStandard(
            name: "CRC-8/DARC",
            size: 8,
            polynomial: 0X39UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/DVB-S2</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_DVBS2 = new CrcStandard(
            name: "CRC-8/DVB-S2",
            size: 8,
            polynomial: 0XD5UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/GSM-A</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_GSMA = new CrcStandard(
            name: "CRC-8/GSM-A",
            size: 8,
            polynomial: 0X1DUL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/GSM-B</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_GSMB = new CrcStandard(
            name: "CRC-8/GSM-B",
            size: 8,
            polynomial: 0X49UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFUL);

        /// <summary>
        /// Represents the <c>CRC-8/HITAG</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_HITAG = new CrcStandard(
            name: "CRC-8/HITAG",
            size: 8,
            polynomial: 0X1DUL,
            initialValue: 0XFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/I-432-1</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_I4321 = new CrcStandard(
            name: "CRC-8/I-432-1",
            size: 8,
            polynomial: 0X07UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X55UL);

        /// <summary>
        /// Represents the <c>CRC-8/I-CODE</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_ICODE = new CrcStandard(
            name: "CRC-8/I-CODE",
            size: 8,
            polynomial: 0X1DUL,
            initialValue: 0XFDUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/LTE</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_LTE = new CrcStandard(
            name: "CRC-8/LTE",
            size: 8,
            polynomial: 0X9BUL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/MAXIM-DOW</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_MAXIMDOW = new CrcStandard(
            name: "CRC-8/MAXIM-DOW",
            size: 8,
            polynomial: 0X31UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/MIFARE-MAD</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_MIFAREMAD = new CrcStandard(
            name: "CRC-8/MIFARE-MAD",
            size: 8,
            polynomial: 0X1DUL,
            initialValue: 0XC7UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/NRSC-5</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_NRSC5 = new CrcStandard(
            name: "CRC-8/NRSC-5",
            size: 8,
            polynomial: 0X31UL,
            initialValue: 0XFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/OPENSAFETY</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_OPENSAFETY = new CrcStandard(
            name: "CRC-8/OPENSAFETY",
            size: 8,
            polynomial: 0X2FUL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/ROHC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_ROHC = new CrcStandard(
            name: "CRC-8/ROHC",
            size: 8,
            polynomial: 0X07UL,
            initialValue: 0XFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/SAE-J1850</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_SAEJ1850 = new CrcStandard(
            name: "CRC-8/SAE-J1850",
            size: 8,
            polynomial: 0X1DUL,
            initialValue: 0XFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFUL);

        /// <summary>
        /// Represents the <c>CRC-8/SMBUS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_SMBUS = new CrcStandard(
            name: "CRC-8/SMBUS",
            size: 8,
            polynomial: 0X07UL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/TECH-3250</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_TECH3250 = new CrcStandard(
            name: "CRC-8/TECH-3250",
            size: 8,
            polynomial: 0X1DUL,
            initialValue: 0XFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-8/WCDMA</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC8_WCDMA = new CrcStandard(
            name: "CRC-8/WCDMA",
            size: 8,
            polynomial: 0X9BUL,
            initialValue: 0X00UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00UL);

        /// <summary>
        /// Represents the <c>CRC-10/ATM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC10_ATM = new CrcStandard(
            name: "CRC-10/ATM",
            size: 10,
            polynomial: 0X233UL,
            initialValue: 0X000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000UL);

        /// <summary>
        /// Represents the <c>CRC-10/CDMA2000</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC10_CDMA2000 = new CrcStandard(
            name: "CRC-10/CDMA2000",
            size: 10,
            polynomial: 0X3D9UL,
            initialValue: 0X3FFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000UL);

        /// <summary>
        /// Represents the <c>CRC-10/GSM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC10_GSM = new CrcStandard(
            name: "CRC-10/GSM",
            size: 10,
            polynomial: 0X175UL,
            initialValue: 0X000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X3FFUL);

        /// <summary>
        /// Represents the <c>CRC-11/FLEXRAY</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC11_FLEXRAY = new CrcStandard(
            name: "CRC-11/FLEXRAY",
            size: 11,
            polynomial: 0X385UL,
            initialValue: 0X01AUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000UL);

        /// <summary>
        /// Represents the <c>CRC-11/UMTS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC11_UMTS = new CrcStandard(
            name: "CRC-11/UMTS",
            size: 11,
            polynomial: 0X307UL,
            initialValue: 0X000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000UL);

        /// <summary>
        /// Represents the <c>CRC-12/CDMA2000</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC12_CDMA2000 = new CrcStandard(
            name: "CRC-12/CDMA2000",
            size: 12,
            polynomial: 0XF13UL,
            initialValue: 0XFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000UL);

        /// <summary>
        /// Represents the <c>CRC-12/DECT</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC12_DECT = new CrcStandard(
            name: "CRC-12/DECT",
            size: 12,
            polynomial: 0X80FUL,
            initialValue: 0X000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000UL);

        /// <summary>
        /// Represents the <c>CRC-12/GSM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC12_GSM = new CrcStandard(
            name: "CRC-12/GSM",
            size: 12,
            polynomial: 0XD31UL,
            initialValue: 0X000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFUL);

        /// <summary>
        /// Represents the <c>CRC-12/UMTS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC12_UMTS = new CrcStandard(
            name: "CRC-12/UMTS",
            size: 12,
            polynomial: 0X80FUL,
            initialValue: 0X000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000UL);

        /// <summary>
        /// Represents the <c>CRC-13/BBC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC13_BBC = new CrcStandard(
            name: "CRC-13/BBC",
            size: 13,
            polynomial: 0X1CF5UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-14/DARC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC14_DARC = new CrcStandard(
            name: "CRC-14/DARC",
            size: 14,
            polynomial: 0X0805UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-14/GSM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC14_GSM = new CrcStandard(
            name: "CRC-14/GSM",
            size: 14,
            polynomial: 0X202DUL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X3FFFUL);

        /// <summary>
        /// Represents the <c>CRC-15/CAN</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC15_CAN = new CrcStandard(
            name: "CRC-15/CAN",
            size: 15,
            polynomial: 0X4599UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-15/MPT1327</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC15_MPT1327 = new CrcStandard(
            name: "CRC-15/MPT1327",
            size: 15,
            polynomial: 0X6815UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0001UL);

        /// <summary>
        /// Represents the <c>CRC-16/ARC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_ARC = new CrcStandard(
            name: "CRC-16/ARC",
            size: 16,
            polynomial: 0X8005UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/CDMA2000</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_CDMA2000 = new CrcStandard(
            name: "CRC-16/CDMA2000",
            size: 16,
            polynomial: 0XC867UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/CMS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_CMS = new CrcStandard(
            name: "CRC-16/CMS",
            size: 16,
            polynomial: 0X8005UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/DDS-110</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_DDS110 = new CrcStandard(
            name: "CRC-16/DDS-110",
            size: 16,
            polynomial: 0X8005UL,
            initialValue: 0X800DUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/DECT-R</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_DECTR = new CrcStandard(
            name: "CRC-16/DECT-R",
            size: 16,
            polynomial: 0X0589UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0001UL);

        /// <summary>
        /// Represents the <c>CRC-16/DECT-X</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_DECTX = new CrcStandard(
            name: "CRC-16/DECT-X",
            size: 16,
            polynomial: 0X0589UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/DNP</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_DNP = new CrcStandard(
            name: "CRC-16/DNP",
            size: 16,
            polynomial: 0X3D65UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/EN-13757</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_EN13757 = new CrcStandard(
            name: "CRC-16/EN-13757",
            size: 16,
            polynomial: 0X3D65UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/GENIBUS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_GENIBUS = new CrcStandard(
            name: "CRC-16/GENIBUS",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/GSM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_GSM = new CrcStandard(
            name: "CRC-16/GSM",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/IBM-3740</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_IBM3740 = new CrcStandard(
            name: "CRC-16/IBM-3740",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/IBM-SDLC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_IBMSDLC = new CrcStandard(
            name: "CRC-16/IBM-SDLC",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/ISO-IEC-14443-3-A</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_ISOIEC144433A = new CrcStandard(
            name: "CRC-16/ISO-IEC-14443-3-A",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0XC6C6UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/KERMIT</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_KERMIT = new CrcStandard(
            name: "CRC-16/KERMIT",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/LJ1200</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_LJ1200 = new CrcStandard(
            name: "CRC-16/LJ1200",
            size: 16,
            polynomial: 0X6F63UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/M17</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_M17 = new CrcStandard(
            name: "CRC-16/M17",
            size: 16,
            polynomial: 0X5935UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/MAXIM-DOW</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_MAXIMDOW = new CrcStandard(
            name: "CRC-16/MAXIM-DOW",
            size: 16,
            polynomial: 0X8005UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/MCRF4XX</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_MCRF4XX = new CrcStandard(
            name: "CRC-16/MCRF4XX",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/MODBUS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_MODBUS = new CrcStandard(
            name: "CRC-16/MODBUS",
            size: 16,
            polynomial: 0X8005UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/NRSC-5</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_NRSC5 = new CrcStandard(
            name: "CRC-16/NRSC-5",
            size: 16,
            polynomial: 0X080BUL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/OPENSAFETY-A</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_OPENSAFETYA = new CrcStandard(
            name: "CRC-16/OPENSAFETY-A",
            size: 16,
            polynomial: 0X5935UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/OPENSAFETY-B</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_OPENSAFETYB = new CrcStandard(
            name: "CRC-16/OPENSAFETY-B",
            size: 16,
            polynomial: 0X755BUL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/PROFIBUS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_PROFIBUS = new CrcStandard(
            name: "CRC-16/PROFIBUS",
            size: 16,
            polynomial: 0X1DCFUL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/RIELLO</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_RIELLO = new CrcStandard(
            name: "CRC-16/RIELLO",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0XB2AAUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/SPI-FUJITSU</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_SPIFUJITSU = new CrcStandard(
            name: "CRC-16/SPI-FUJITSU",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0X1D0FUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/T10-DIF</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_T10DIF = new CrcStandard(
            name: "CRC-16/T10-DIF",
            size: 16,
            polynomial: 0X8BB7UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/TELEDISK</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_TELEDISK = new CrcStandard(
            name: "CRC-16/TELEDISK",
            size: 16,
            polynomial: 0XA097UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/TMS37157</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_TMS37157 = new CrcStandard(
            name: "CRC-16/TMS37157",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0X89ECUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/UMTS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_UMTS = new CrcStandard(
            name: "CRC-16/UMTS",
            size: 16,
            polynomial: 0X8005UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-16/USB</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_USB = new CrcStandard(
            name: "CRC-16/USB",
            size: 16,
            polynomial: 0X8005UL,
            initialValue: 0XFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-16/XMODEM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC16_XMODEM = new CrcStandard(
            name: "CRC-16/XMODEM",
            size: 16,
            polynomial: 0X1021UL,
            initialValue: 0X0000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000UL);

        /// <summary>
        /// Represents the <c>CRC-17/CAN-FD</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC17_CANFD = new CrcStandard(
            name: "CRC-17/CAN-FD",
            size: 17,
            polynomial: 0X1685BUL,
            initialValue: 0X00000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00000UL);

        /// <summary>
        /// Represents the <c>CRC-21/CAN-FD</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC21_CANFD = new CrcStandard(
            name: "CRC-21/CAN-FD",
            size: 21,
            polynomial: 0X102899UL,
            initialValue: 0X000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000UL);

        /// <summary>
        /// Represents the <c>CRC-24/BLE</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_BLE = new CrcStandard(
            name: "CRC-24/BLE",
            size: 24,
            polynomial: 0X00065BUL,
            initialValue: 0X555555UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000UL);

        /// <summary>
        /// Represents the <c>CRC-24/FLEXRAY-A</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_FLEXRAYA = new CrcStandard(
            name: "CRC-24/FLEXRAY-A",
            size: 24,
            polynomial: 0X5D6DCBUL,
            initialValue: 0XFEDCBAUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000UL);

        /// <summary>
        /// Represents the <c>CRC-24/FLEXRAY-B</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_FLEXRAYB = new CrcStandard(
            name: "CRC-24/FLEXRAY-B",
            size: 24,
            polynomial: 0X5D6DCBUL,
            initialValue: 0XABCDEFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000UL);

        /// <summary>
        /// Represents the <c>CRC-24/INTERLAKEN</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_INTERLAKEN = new CrcStandard(
            name: "CRC-24/INTERLAKEN",
            size: 24,
            polynomial: 0X328B63UL,
            initialValue: 0XFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-24/LTE-A</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_LTEA = new CrcStandard(
            name: "CRC-24/LTE-A",
            size: 24,
            polynomial: 0X864CFBUL,
            initialValue: 0X000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000UL);

        /// <summary>
        /// Represents the <c>CRC-24/LTE-B</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_LTEB = new CrcStandard(
            name: "CRC-24/LTE-B",
            size: 24,
            polynomial: 0X800063UL,
            initialValue: 0X000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000UL);

        /// <summary>
        /// Represents the <c>CRC-24/OPENPGP</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_OPENPGP = new CrcStandard(
            name: "CRC-24/OPENPGP",
            size: 24,
            polynomial: 0X864CFBUL,
            initialValue: 0XB704CEUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000UL);

        /// <summary>
        /// Represents the <c>CRC-24/OS-9</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC24_OS9 = new CrcStandard(
            name: "CRC-24/OS-9",
            size: 24,
            polynomial: 0X800063UL,
            initialValue: 0XFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-30/CDMA</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC30_CDMA = new CrcStandard(
            name: "CRC-30/CDMA",
            size: 30,
            polynomial: 0X2030B9C7UL,
            initialValue: 0X3FFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X3FFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-31/PHILIPS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC31_PHILIPS = new CrcStandard(
            name: "CRC-31/PHILIPS",
            size: 31,
            polynomial: 0X04C11DB7UL,
            initialValue: 0X7FFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X7FFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-32/AIXM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_AIXM = new CrcStandard(
            name: "CRC-32/AIXM",
            size: 32,
            polynomial: 0X814141ABUL,
            initialValue: 0X00000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00000000UL);

        /// <summary>
        /// Represents the <c>CRC-32/AUTOSAR</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_AUTOSAR = new CrcStandard(
            name: "CRC-32/AUTOSAR",
            size: 32,
            polynomial: 0XF4ACFB13UL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-32/BASE91-D</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_BASE91D = new CrcStandard(
            name: "CRC-32/BASE91-D",
            size: 32,
            polynomial: 0XA833982BUL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-32/BZIP2</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_BZIP2 = new CrcStandard(
            name: "CRC-32/BZIP2",
            size: 32,
            polynomial: 0X04C11DB7UL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-32/CD-ROM-EDC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_CDROMEDC = new CrcStandard(
            name: "CRC-32/CD-ROM-EDC",
            size: 32,
            polynomial: 0X8001801BUL,
            initialValue: 0X00000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00000000UL);

        /// <summary>
        /// Represents the <c>CRC-32/CKSUM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_CKSUM = new CrcStandard(
            name: "CRC-32/CKSUM",
            size: 32,
            polynomial: 0X04C11DB7UL,
            initialValue: 0X00000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-32/ISCSI</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_ISCSI = new CrcStandard(
            name: "CRC-32/ISCSI",
            size: 32,
            polynomial: 0X1EDC6F41UL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-32/ISO-HDLC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_ISOHDLC = new CrcStandard(
            name: "CRC-32/ISO-HDLC",
            size: 32,
            polynomial: 0X04C11DB7UL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-32/JAMCRC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_JAMCRC = new CrcStandard(
            name: "CRC-32/JAMCRC",
            size: 32,
            polynomial: 0X04C11DB7UL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00000000UL);

        /// <summary>
        /// Represents the <c>CRC-32/MEF</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_MEF = new CrcStandard(
            name: "CRC-32/MEF",
            size: 32,
            polynomial: 0X741B8CD7UL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00000000UL);

        /// <summary>
        /// Represents the <c>CRC-32/MPEG-2</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_MPEG2 = new CrcStandard(
            name: "CRC-32/MPEG-2",
            size: 32,
            polynomial: 0X04C11DB7UL,
            initialValue: 0XFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00000000UL);

        /// <summary>
        /// Represents the <c>CRC-32/XFER</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC32_XFER = new CrcStandard(
            name: "CRC-32/XFER",
            size: 32,
            polynomial: 0X000000AFUL,
            initialValue: 0X00000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X00000000UL);

        /// <summary>
        /// Represents the <c>CRC-40/GSM</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC40_GSM = new CrcStandard(
            name: "CRC-40/GSM",
            size: 40,
            polynomial: 0X0004820009UL,
            initialValue: 0X0000000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-64/ECMA-182</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_ECMA182 = new CrcStandard(
            name: "CRC-64/ECMA-182",
            size: 64,
            polynomial: 0X42F0E1EBA9EA3693UL,
            initialValue: 0X0000000000000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000000000000000UL);

        /// <summary>
        /// Represents the <c>CRC-64/GO-ISO</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_GOISO = new CrcStandard(
            name: "CRC-64/GO-ISO",
            size: 64,
            polynomial: 0X000000000000001BUL,
            initialValue: 0XFFFFFFFFFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-64/JONES</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_JONES = new CrcStandard(
            name: "CRC-64/JONES",
            size: 64,
            polynomial: 0xAD93D23594C935A9UL,
            initialValue: 0xFFFFFFFFFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0x0000000000000000UL);

        /// <summary>
        /// Represents the <c>CRC-64/MS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_MS = new CrcStandard(
            name: "CRC-64/MS",
            size: 64,
            polynomial: 0X259C84CBA6426349UL,
            initialValue: 0XFFFFFFFFFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000000000000000UL);

        /// <summary>
        /// Represents the <c>CRC-64/NVME</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_NVME = new CrcStandard(
            name: "CRC-64/NVME",
            size: 64,
            polynomial: 0XAD93D23594C93659UL,
            initialValue: 0XFFFFFFFFFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-64/REDIS</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_REDIS = new CrcStandard(
            name: "CRC-64/REDIS",
            size: 64,
            polynomial: 0XAD93D23594C935A9UL,
            initialValue: 0X0000000000000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X0000000000000000UL);

        /// <summary>
        /// Represents the <c>CRC-64/WE</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_WE = new CrcStandard(
            name: "CRC-64/WE",
            size: 64,
            polynomial: 0X42F0E1EBA9EA3693UL,
            initialValue: 0XFFFFFFFFFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-64/XZ</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC64_XZ = new CrcStandard(
            name: "CRC-64/XZ",
            size: 64,
            polynomial: 0X42F0E1EBA9EA3693UL,
            initialValue: 0XFFFFFFFFFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0XFFFFFFFFFFFFFFFFUL);

        /// <summary>
        /// Represents the <c>CRC-82/DARC</c> CRC standard.
        /// </summary>
        public static readonly CrcStandard CRC82_DARC = new CrcStandard(
            name: "CRC-82/DARC",
            size: 82,
            polynomial: 0X0308C0111011401440411UL,
            initialValue: 0X000000000000000000000UL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0X000000000000000000000UL);

        private static readonly CrcStandard[] s_all = new[]
        {
            CRC3_GSM,
            CRC3_ROHC,
            CRC4_G704,
            CRC4_INTERLAKEN,
            CRC5_EPCC1G2,
            CRC5_G704,
            CRC5_USB,
            CRC6_CDMA2000A,
            CRC6_CDMA2000B,
            CRC6_DARC,
            CRC6_G704,
            CRC6_GSM,
            CRC7_MMC,
            CRC7_ROHC,
            CRC7_UMTS,
            CRC8_AUTOSAR,
            CRC8_CDMA2000,
            CRC8_DARC,
            CRC8_DVBS2,
            CRC8_GSMA,
            CRC8_GSMB,
            CRC8_HITAG,
            CRC8_I4321,
            CRC8_ICODE,
            CRC8_LTE,
            CRC8_MAXIMDOW,
            CRC8_MIFAREMAD,
            CRC8_NRSC5,
            CRC8_OPENSAFETY,
            CRC8_ROHC,
            CRC8_SAEJ1850,
            CRC8_SMBUS,
            CRC8_TECH3250,
            CRC8_WCDMA,
            CRC10_ATM,
            CRC10_CDMA2000,
            CRC10_GSM,
            CRC11_FLEXRAY,
            CRC11_UMTS,
            CRC12_CDMA2000,
            CRC12_DECT,
            CRC12_GSM,
            CRC12_UMTS,
            CRC13_BBC,
            CRC14_DARC,
            CRC14_GSM,
            CRC15_CAN,
            CRC15_MPT1327,
            CRC16_ARC,
            CRC16_CDMA2000,
            CRC16_CMS,
            CRC16_DDS110,
            CRC16_DECTR,
            CRC16_DECTX,
            CRC16_DNP,
            CRC16_EN13757,
            CRC16_GENIBUS,
            CRC16_GSM,
            CRC16_IBM3740,
            CRC16_IBMSDLC,
            CRC16_ISOIEC144433A,
            CRC16_KERMIT,
            CRC16_LJ1200,
            CRC16_M17,
            CRC16_MAXIMDOW,
            CRC16_MCRF4XX,
            CRC16_MODBUS,
            CRC16_NRSC5,
            CRC16_OPENSAFETYA,
            CRC16_OPENSAFETYB,
            CRC16_PROFIBUS,
            CRC16_RIELLO,
            CRC16_SPIFUJITSU,
            CRC16_T10DIF,
            CRC16_TELEDISK,
            CRC16_TMS37157,
            CRC16_UMTS,
            CRC16_USB,
            CRC16_XMODEM,
            CRC17_CANFD,
            CRC21_CANFD,
            CRC24_BLE,
            CRC24_FLEXRAYA,
            CRC24_FLEXRAYB,
            CRC24_INTERLAKEN,
            CRC24_LTEA,
            CRC24_LTEB,
            CRC24_OPENPGP,
            CRC24_OS9,
            CRC30_CDMA,
            CRC31_PHILIPS,
            CRC32_AIXM,
            CRC32_AUTOSAR,
            CRC32_BASE91D,
            CRC32_BZIP2,
            CRC32_CDROMEDC,
            CRC32_CKSUM,
            CRC32_ISCSI,
            CRC32_ISOHDLC,
            CRC32_JAMCRC,
            CRC32_MEF,
            CRC32_MPEG2,
            CRC32_XFER,
            CRC40_GSM,
            CRC64_ECMA182,
            CRC64_GOISO,
            CRC64_JONES,
            CRC64_MS,
            CRC64_NVME,
            CRC64_REDIS,
            CRC64_WE,
            CRC64_XZ,
            CRC82_DARC,
        };

        private static readonly Dictionary<string, CrcStandard> s_byName = BuildNameLookup();

        /// <summary>
        /// Gets an immutable snapshot of every CRC standard defined in the catalogue, in declaration order.
        /// </summary>
        /// <value>A read-only list containing all 113 <see cref="CrcStandard" /> entries.</value>
        public static IReadOnlyList<CrcStandard> All => s_all;

        /// <summary>
        /// Resolves a catalogue entry by its canonical name (for example <c>"CRC-32/ISO-HDLC"</c>).
        /// </summary>
        /// <param name="name">The canonical CRC standard name. Comparison is ordinal and case-sensitive.</param>
        /// <returns>The matching <see cref="CrcStandard" /> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="KeyNotFoundException">No catalogue entry with the specified <paramref name="name" /> exists.</exception>
        public static CrcStandard FromName(string name)
        {
            ThrowHelper.ThrowIfNull(name);
            if (!s_byName.TryGetValue(name, out CrcStandard? result))
                throw new KeyNotFoundException($"No CRC standard with the name '{name}' exists in the catalogue.");
            return result;
        }

        /// <summary>
        /// Attempts to resolve a catalogue entry by its canonical name without throwing.
        /// </summary>
        /// <param name="name">The canonical CRC standard name to look up.</param>
        /// <param name="standard">When this method returns, contains the matching <see cref="CrcStandard" /> if found; otherwise, <see langword="null" />.</param>
        /// <returns><see langword="true" /> if a catalogue entry was found; otherwise, <see langword="false" />.</returns>
        public static bool TryFromName(string? name, out CrcStandard? standard)
        {
            if (name is null)
            {
                standard = null;
                return false;
            }

            return s_byName.TryGetValue(name, out standard);
        }

        private static Dictionary<string, CrcStandard> BuildNameLookup()
        {
            var map = new Dictionary<string, CrcStandard>(s_all.Length, StringComparer.Ordinal);
            foreach (CrcStandard entry in s_all)
                map[entry.Name] = entry;
            return map;
        }
    }
}
