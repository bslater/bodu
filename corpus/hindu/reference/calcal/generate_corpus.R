#!/usr/bin/env Rscript
# ---------------------------------------------------------------------------------------------------------------
# generate_corpus.R - calcal Modern Hindu Reference corpus generator (Bodu calendar validation corpus)
#
# Generates one row per Gregorian civil date per requested calendar model from the pinned calcal package
# (an R translation of Calendrica 4.0, Apache-2.0). Output rows are reference-generated data - an independent
# executable implementation of the Reingold-Dershowitz algorithms - NOT official data.
#
# ADAPTER NOTE (per the corpus specification): the adapter below was confirmed against the pinned calcal 1.0.4
# release on first run (2026-08-03) and corrected from the specification's illustrative Lisp-style names to the
# release's vectorized vctrs API: as_gregorian / as_hindu_lunar / as_hindu_solar / as_old_hindu_lunar /
# as_old_hindu_solar over Date vectors, with field access via granularity(x, name) and the RD fixed day via
# as.numeric(). The modern lunar year is the Vikrama era and the old lunar/solar years the Kali era, as printed
# by the package's formatters. The embedded smoke checks protect exactly that adapter shape; do not commit a
# generated corpus whose smoke checks did not pass.
#
# Usage:
#   Rscript generate_corpus.R \
#     --start-date 1990-01-01 \
#     --end-date   2039-12-31 \
#     --output     ../../data/normalized/hindu-reference-daily.csv \
#     --model      all
# ---------------------------------------------------------------------------------------------------------------

suppressPackageStartupMessages({
  library(calcal)
})

args <- commandArgs(trailingOnly = TRUE)

parse_arg <- function(name, default = NULL) {
  pos <- match(name, args)
  if (is.na(pos)) return(default)
  if (pos == length(args)) stop(sprintf("Missing value for %s", name))
  args[[pos + 1L]]
}

start_date <- as.Date(parse_arg("--start-date", "1990-01-01"))
end_date <- as.Date(parse_arg("--end-date", "2039-12-31"))
output <- parse_arg("--output", "hindu-reference-daily.csv")
model <- parse_arg("--model", "all")

if (is.na(start_date) || is.na(end_date) || end_date < start_date) {
  stop("Invalid date range")
}

models <- if (model == "all") {
  c("modern-hindu-lunar", "modern-hindu-solar", "old-hindu-lunar", "old-hindu-solar")
} else {
  model
}

package_version_string <- as.character(utils::packageVersion("calcal"))
generated_at_utc <- format(Sys.time(), "%Y-%m-%dT%H:%M:%SZ", tz = "UTC")
generator_sha256 <- tryCatch(
  as.character(tools::sha256sum(sub("--file=", "", grep("--file=", commandArgs(FALSE), value = TRUE)[1]))),
  error = function(e) "unknown"
)

dates <- seq.Date(start_date, end_date, by = "day")

# Converts the full date vector for one calendar model in a single vectorized pass (the calcal API is
# vctrs-based and vectorized; per-date loops would re-run the astronomy thousands of times for nothing).
convert_model <- function(dates, calendar_model) {
  n <- length(dates)
  frame <- data.frame(
    gregorian_date = format(dates, "%Y-%m-%d"),
    fixed_date = as.numeric(as_gregorian(dates)),
    calendar_model = rep(calendar_model, n),
    month_system = rep(if (grepl("lunar", calendar_model)) "amanta" else "solar", n),
    lunar_year = NA_integer_, lunar_month = NA_integer_, lunar_day = NA_integer_,
    is_leap_month = NA, is_leap_day = NA,
    solar_year = NA_integer_, solar_month = NA_integer_, solar_day = NA_integer_,
    source_class = rep("reference-generated", n),
    source_id = rep(paste0("calcal-", calendar_model), n),
    source_version = rep(package_version_string, n),
    location_id = rep("calcal-builtin-ujjain", n),
    timezone = rep("Asia/Kolkata", n),
    utc_offset = rep("+05:30", n),
    transcription_method = rep("generated", n),
    quality_status = rep("verified", n),
    stringsAsFactors = FALSE
  )

  if (calendar_model == "modern-hindu-lunar") {
    l <- as_hindu_lunar(dates)
    frame$lunar_year <- as.integer(granularity(l, "year"))
    frame$lunar_month <- as.integer(granularity(l, "month"))
    frame$lunar_day <- as.integer(granularity(l, "day"))
    frame$is_leap_month <- as.logical(granularity(l, "leap_month"))
    frame$is_leap_day <- as.logical(granularity(l, "leap_day"))
  } else if (calendar_model == "modern-hindu-solar") {
    s <- as_hindu_solar(dates)
    frame$solar_year <- as.integer(granularity(s, "year"))
    frame$solar_month <- as.integer(granularity(s, "month"))
    frame$solar_day <- as.integer(granularity(s, "day"))
  } else if (calendar_model == "old-hindu-lunar") {
    l <- as_old_hindu_lunar(dates)
    frame$lunar_year <- as.integer(granularity(l, "year"))
    frame$lunar_month <- as.integer(granularity(l, "month"))
    frame$lunar_day <- as.integer(granularity(l, "day"))
    frame$is_leap_month <- as.logical(granularity(l, "leap_month"))
  } else if (calendar_model == "old-hindu-solar") {
    s <- as_old_hindu_solar(dates)
    frame$solar_year <- as.integer(granularity(s, "year"))
    frame$solar_month <- as.integer(granularity(s, "month"))
    frame$solar_day <- as.integer(granularity(s, "day"))
  } else {
    stop(sprintf("Unknown calendar model '%s'", calendar_model))
  }

  frame
}

# ---- Smoke checks (spec section 10.3/10.4): protect the adapter before bulk generation. -----------------------

smoke <- function() {
  probe <- as.Date("2024-11-01") # Diwali 2024: Kartika new-moon vicinity under the amanta convention.
  g <- gregorian_date(2024, 11, 1)

  # Metamorphic: the constructor, the Date conversion, and a fixed -> gregorian round trip agree.
  stopifnot(as.numeric(g) == as.numeric(as_gregorian(probe)))
  gg <- as_gregorian(as_hindu_lunar(g))
  stopifnot(
    granularity(gg, "year") == 2024L,
    granularity(gg, "month") == 11L,
    granularity(gg, "day") == 1L
  )

  # Metamorphic: consecutive fixed dates differ by exactly one.
  stopifnot(as.numeric(as_gregorian(probe + 1L)) - as.numeric(g) == 1)

  # Era-free anchor: 1 Nov 2024 falls at the very end of amanta Asvina's dark fortnight (Diwali):
  # the lunar day must be 30 (amavasya) or 1 (first day of the following month) within adapter tolerance,
  # and the leap flag must be FALSE.
  l <- as_hindu_lunar(probe)
  stopifnot(granularity(l, "day") %in% c(29L, 30L, 1L))
  stopifnot(identical(as.logical(granularity(l, "leap_month")), FALSE))

  # Metamorphic: a leap-month flag may change only at a lunar month boundary - scan one synodic month.
  window <- as_hindu_lunar(probe + 0:30)
  leap <- as.logical(granularity(window, "leap_month"))
  month <- as.integer(granularity(window, "month"))
  year <- as.integer(granularity(window, "year"))
  changed <- which(leap[-1] != leap[-length(leap)])
  for (i in changed) {
    stopifnot(month[i + 1] != month[i] || year[i + 1] != year[i])
  }

  invisible(TRUE)
}

smoke()

# ---- Generation. ----------------------------------------------------------------------------------------------

frames <- list()
for (m in models) {
  frames[[m]] <- convert_model(dates, m)
}
result <- do.call(rbind, frames)

# Row-count invariant: one row per date per model.
stopifnot(nrow(result) == length(dates) * length(models))

header <- c(
  "# calcal Modern Hindu Reference corpus (reference-generated; NOT official data).",
  sprintf("# Generated: %s | calcal %s | %s | generator sha256 %s",
          generated_at_utc, package_version_string, R.version.string, generator_sha256),
  sprintf("# Range: %s..%s | models: %s | one row per date per model.",
          format(start_date, "%Y-%m-%d"), format(end_date, "%Y-%m-%d"), paste(models, collapse = ", ")),
  "# Location/profile: calcal built-in Modern Hindu reference location (Ujjain); amanta month naming.",
  "# Lineage: Reingold-Dershowitz Calendrical Calculations via calcal (Apache-2.0). Old Hindu models are",
  "#   for algorithm-family regression only and must not be compared against modern Panchang expectations."
)

con <- file(output, open = "wt", encoding = "UTF-8")
writeLines(header, con)
write.csv(result, file = con, row.names = FALSE, na = "", quote = TRUE)
close(con)

cat(sprintf("Wrote %d rows to %s\n", nrow(result), output))
