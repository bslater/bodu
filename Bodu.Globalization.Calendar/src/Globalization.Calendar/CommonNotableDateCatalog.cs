// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CommonNotableDateCatalog.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Enumerates the shared, reusable notable-date catalogues bundled with the library, providing a strongly-typed name
/// for each so callers can resolve a catalogue through
/// <see cref="CommonNotableDateResources.Resolve(CommonNotableDateCatalog)" /> without spelling its raw resource name.
/// </summary>
/// <remarks>
/// Each member maps one-to-one to an embedded catalogue whose bare resource name is its kebab-case equivalent (for
/// example <see cref="GlobalCore" /> maps to <c>global-core</c> and <see cref="ChristianWestern" /> to
/// <c>christian-western</c>). Use <see cref="CommonNotableDateResources.GetResourceName(CommonNotableDateCatalog)" />
/// to obtain the raw name for an <c>Import</c> directive.
/// </remarks>
public enum CommonNotableDateCatalog
{
    /// <summary>
    /// The Roman Catholic observances catalogue (<c>catholic</c>).
    /// </summary>
    Catholic,

    /// <summary>
    /// The Anglican Christian observances catalogue (<c>christian-anglican</c>).
    /// </summary>
    ChristianAnglican,

    /// <summary>
    /// The Oriental Orthodox Christian observances catalogue (<c>christian-oriental-orthodox</c>).
    /// </summary>
    ChristianOrientalOrthodox,

    /// <summary>
    /// The Eastern Orthodox Christian observances catalogue (<c>christian-orthodox</c>).
    /// </summary>
    ChristianOrthodox,

    /// <summary>
    /// The Protestant Christian observances catalogue (<c>christian-protestant</c>).
    /// </summary>
    ChristianProtestant,

    /// <summary>
    /// The Western Christian observances catalogue (<c>christian-western</c>).
    /// </summary>
    ChristianWestern,

    /// <summary>
    /// The minimal default catalogue of baseline observances (<c>default-minimal</c>).
    /// </summary>
    DefaultMinimal,

    /// <summary>
    /// The aggregate catalogue combining every bundled concept (<c>global-all</c>).
    /// </summary>
    GlobalAll,

    /// <summary>
    /// The catalogue of anchor dates that other rules reference (<c>global-anchors</c>).
    /// </summary>
    GlobalAnchors,

    /// <summary>
    /// The animal-themed awareness catalogue (<c>global-animals</c>).
    /// </summary>
    GlobalAnimals,

    /// <summary>
    /// The Baháʼí faith observances catalogue (<c>global-bahai</c>).
    /// </summary>
    GlobalBahai,

    /// <summary>
    /// The Buddhist observances catalogue (<c>global-buddhist</c>).
    /// </summary>
    GlobalBuddhist,

    /// <summary>
    /// The core civil observances catalogue — New Year's Day and similar universal dates (<c>global-core</c>).
    /// </summary>
    GlobalCore,

    /// <summary>
    /// The cultural observances catalogue (<c>global-cultural</c>).
    /// </summary>
    GlobalCultural,

    /// <summary>
    /// The education-themed observances catalogue (<c>global-education</c>).
    /// </summary>
    GlobalEducation,

    /// <summary>
    /// The environmental observances catalogue (<c>global-environment</c>).
    /// </summary>
    GlobalEnvironment,

    /// <summary>
    /// The family and social observances catalogue (<c>global-family-social</c>).
    /// </summary>
    GlobalFamilySocial,

    /// <summary>
    /// The family observances catalogue (<c>global-family</c>).
    /// </summary>
    GlobalFamily,

    /// <summary>
    /// The food-themed observances catalogue (<c>global-food</c>).
    /// </summary>
    GlobalFood,

    /// <summary>
    /// The health-themed observances catalogue (<c>global-health</c>).
    /// </summary>
    GlobalHealth,

    /// <summary>
    /// The Hindu observances catalogue (<c>global-hindu</c>).
    /// </summary>
    GlobalHindu,

    /// <summary>
    /// The Islamic observances catalogue computed on the Umm al-Qura calendar (<c>global-islamic-umm-al-qura</c>).
    /// </summary>
    GlobalIslamicUmmAlQura,

    /// <summary>
    /// The Islamic observances catalogue (<c>global-islamic</c>).
    /// </summary>
    GlobalIslamic,

    /// <summary>
    /// The Jain observances catalogue (<c>global-jain</c>).
    /// </summary>
    GlobalJain,

    /// <summary>
    /// The Jewish observances catalogue (<c>global-jewish</c>).
    /// </summary>
    GlobalJewish,

    /// <summary>
    /// The lunar observances catalogue (<c>global-lunar</c>).
    /// </summary>
    GlobalLunar,

    /// <summary>
    /// The multi-day normalization helpers catalogue (<c>global-multiday-normalization</c>).
    /// </summary>
    GlobalMultidayNormalization,

    /// <summary>
    /// The Persian (Solar Hijri) observances catalogue (<c>global-persian</c>).
    /// </summary>
    GlobalPersian,

    /// <summary>
    /// The remembrance observances catalogue (<c>global-remembrance</c>).
    /// </summary>
    GlobalRemembrance,

    /// <summary>
    /// The science-themed observances catalogue (<c>global-science</c>).
    /// </summary>
    GlobalScience,

    /// <summary>
    /// The Sikh observances catalogue (<c>global-sikh</c>).
    /// </summary>
    GlobalSikh,

    /// <summary>
    /// The social-awareness observances catalogue (<c>global-social</c>).
    /// </summary>
    GlobalSocial,

    /// <summary>
    /// The United Nations international days catalogue (<c>global-un</c>).
    /// </summary>
    GlobalUn,

    /// <summary>
    /// The Zoroastrian observances catalogue (<c>global-zoroastrian</c>).
    /// </summary>
    GlobalZoroastrian,
}
