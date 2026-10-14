// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CommonNotableDateCatalog.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Enumerates the shared, reusable notable-date catalogs bundled with the library, providing a strongly-typed name for
/// each so callers can resolve a catalog through
/// <see cref="CommonNotableDateResources.Resolve(CommonNotableDateCatalog)" /> without spelling its raw resource name.
/// </summary>
/// <remarks>
/// Each member maps one-to-one to an embedded catalog whose bare resource name is its kebab-case equivalent (for
/// example <see cref="GlobalCore" /> maps to <c>global-core</c> and <see cref="ChristianWestern" /> to
/// <c>christian-western</c>). Use <see cref="CommonNotableDateResources.GetResourceName(CommonNotableDateCatalog)" />
/// to obtain the raw name for an <c>Import</c> directive.
/// </remarks>
public enum CommonNotableDateCatalog
{
    /// <summary>
    /// The Roman Catholic observances catalog (<c>catholic</c>).
    /// </summary>
    Catholic,

    /// <summary>
    /// The Anglican Christian observances catalog (<c>christian-anglican</c>).
    /// </summary>
    ChristianAnglican,

    /// <summary>
    /// The Oriental Orthodox Christian observances catalog (<c>christian-oriental-orthodox</c>).
    /// </summary>
    ChristianOrientalOrthodox,

    /// <summary>
    /// The Eastern Orthodox Christian observances catalog (<c>christian-orthodox</c>).
    /// </summary>
    ChristianOrthodox,

    /// <summary>
    /// The Protestant Christian observances catalog (<c>christian-protestant</c>).
    /// </summary>
    ChristianProtestant,

    /// <summary>
    /// The Western Christian observances catalog (<c>christian-western</c>).
    /// </summary>
    ChristianWestern,

    /// <summary>
    /// The minimal default catalog of baseline observances (<c>default-minimal</c>).
    /// </summary>
    DefaultMinimal,

    /// <summary>
    /// The aggregate catalog combining every bundled concept (<c>global-all</c>).
    /// </summary>
    GlobalAll,

    /// <summary>
    /// The catalog of anchor dates that other rules reference (<c>global-anchors</c>).
    /// </summary>
    GlobalAnchors,

    /// <summary>
    /// The animal-themed awareness catalog (<c>global-animals</c>).
    /// </summary>
    GlobalAnimals,

    /// <summary>
    /// The Baháʼí faith observances catalog (<c>global-bahai</c>).
    /// </summary>
    GlobalBahai,

    /// <summary>
    /// The Buddhist observances catalog (<c>global-buddhist</c>).
    /// </summary>
    GlobalBuddhist,

    /// <summary>
    /// The core civil observances catalog - New Year's Day and similar universal dates (<c>global-core</c>).
    /// </summary>
    GlobalCore,

    /// <summary>
    /// The cultural observances catalog (<c>global-cultural</c>).
    /// </summary>
    GlobalCultural,

    /// <summary>
    /// The education-themed observances catalog (<c>global-education</c>).
    /// </summary>
    GlobalEducation,

    /// <summary>
    /// The environmental observances catalog (<c>global-environment</c>).
    /// </summary>
    GlobalEnvironment,

    /// <summary>
    /// The family and social observances catalog (<c>global-family-social</c>).
    /// </summary>
    GlobalFamilySocial,

    /// <summary>
    /// The family observances catalog (<c>global-family</c>).
    /// </summary>
    GlobalFamily,

    /// <summary>
    /// The food-themed observances catalog (<c>global-food</c>).
    /// </summary>
    GlobalFood,

    /// <summary>
    /// The health-themed observances catalog (<c>global-health</c>).
    /// </summary>
    GlobalHealth,

    /// <summary>
    /// The Hindu observances catalog (<c>global-hindu</c>).
    /// </summary>
    GlobalHindu,

    /// <summary>
    /// The Islamic observances catalog computed on the Umm al-Qura calendar (<c>global-islamic-umm-al-qura</c>).
    /// </summary>
    GlobalIslamicUmmAlQura,

    /// <summary>
    /// The Islamic observances catalog (<c>global-islamic</c>).
    /// </summary>
    GlobalIslamic,

    /// <summary>
    /// The Jain observances catalog (<c>global-jain</c>).
    /// </summary>
    GlobalJain,

    /// <summary>
    /// The Jewish observances catalog (<c>global-jewish</c>).
    /// </summary>
    GlobalJewish,

    /// <summary>
    /// The lunar observances catalog (<c>global-lunar</c>).
    /// </summary>
    GlobalLunar,

    /// <summary>
    /// The multi-day normalization helpers catalog (<c>global-multiday-normalization</c>).
    /// </summary>
    GlobalMultidayNormalization,

    /// <summary>
    /// The Persian (Solar Hijri) observances catalog (<c>global-persian</c>).
    /// </summary>
    GlobalPersian,

    /// <summary>
    /// The remembrance observances catalog (<c>global-remembrance</c>).
    /// </summary>
    GlobalRemembrance,

    /// <summary>
    /// The science-themed observances catalog (<c>global-science</c>).
    /// </summary>
    GlobalScience,

    /// <summary>
    /// The Sikh observances catalog (<c>global-sikh</c>).
    /// </summary>
    GlobalSikh,

    /// <summary>
    /// The social-awareness observances catalog (<c>global-social</c>).
    /// </summary>
    GlobalSocial,

    /// <summary>
    /// The United Nations international days catalog (<c>global-un</c>).
    /// </summary>
    GlobalUn,

    /// <summary>
    /// The Zoroastrian observances catalog (<c>global-zoroastrian</c>).
    /// </summary>
    GlobalZoroastrian,
}
