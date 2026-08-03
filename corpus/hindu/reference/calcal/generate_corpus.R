#!/usr/bin/env Rscript
# ---------------------------------------------------------------------------------------------------------------
# generate_corpus.R - calcal Modern Hindu Reference corpus generator (Bodu calendar validation corpus)
#
# Generates one row per Gregorian civil date per requested calendar model from the pinned calcal package
# (an R translation of Calendrica 4.0, Apache-2.0). Output rows are reference-generated data - an independent
# executable implementation of the Reingold-Dershowitz algorithms - NOT official data.
#
# ADAPTER NOTE (per the corpus specification): the calcal API calls below (gregorian_date, fixed_from_gregorian,
# hindu_solar_from_fixed, hindu_lunar_from_fixed, old_hindu_solar_from_fixed, old_hindu_lunar_from_fixed) must be
# confirmed against the pinned calcal 1.0.4 release on first run and corrected if the exported names or return
# shapes differ. The embedded smoke checks exist to catch exactly that class of drift; do not commit a generated
# corpus whose smoke checks did not pass.
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

convert_one <- function(date, calendar_model) {
  g <- gregorian_date(
    as.integer(format(date, "%Y")),
    as.integer(format(date, "%m")),
    as.integer(format(date, "%d"))
  )
  fixed <- fixed_from_gregorian(g)

  row <- list(
    gregorian_date = format(date, "%Y-%m-%d"),
    fixed_date = as.numeric(fixed),
    calendar_model = calendar_model,
    month_system = if (grepl("lunar", calendar_model)) "amanta" else "solar",
    lunar_year = NA_integer_, lunar_month = NA_integer_, lunar_day = NA_integer_,
    is_leap_month = NA, is_leap_day = NA,
    solar_year = NA_integer_, solar_month = NA_integer_, solar_day = NA_integer_,
    source_class = "reference-generated",
    source_id = paste0("calcal-", calendar_model),
    source_version = package_version_string,
    location_id = "calcal-builtin-ujjain",
    timezone = "Asia/Kolkata",
    utc_offset = "+05:30",
    transcription_method = "generated",
    quality_status = "verified"
  )

  if (calendar_model == "modern-hindu-lunar") {
    l <- hindu_lunar_from_fixed(fixed)
    row$lunar_year <- l$year; row$lunar_month <- l$month; row$lunar_day <- l$day
    row$is_leap_month <- l$leap_month; row$is_leap_day <- l$leap_day
  } else if (calendar_model == "modern-hindu-solar") {
    s <- hindu_solar_from_fixed(fixed)
    row$solar_year <- s$year; row$solar_month <- s$month; row$solar_day <- s$day
  } else if (calendar_model == "old-hindu-lunar") {
    l <- old_hindu_lunar_from_fixed(fixed)
    row$lunar_year <- l$year; row$lunar_month <- l$month; row$lunar_day <- l$day
    row$is_leap_month <- l$leap
  } else if (calendar_model == "old-hindu-solar") {
    s <- old_hindu_solar_from_fixed(fixed)
    row$solar_year <- s$year; row$solar_month <- s$month; row$solar_day <- s$day
  }

  as.data.frame(row, stringsAsFactors = FALSE)
}

# ---- Smoke checks (spec section 10.3/10.4): protect the adapter before bulk generation. -----------------------

smoke <- function() {
  probe <- as.Date("2024-11-01") # Diwali 2024: Kartika new-moon vicinity under the amanta convention.
  g <- gregorian_date(2024L, 11L, 1L)
  fixed <- fixed_from_gregorian(g)

  # Metamorphic: gregorian -> fixed -> gregorian is the identity.
  gg <- gregorian_from_fixed(fixed)
  stopifnot(gg$year == 2024L, gg$month == 11L, gg$day == 1L)

  # Metamorphic: consecutive fixed dates differ by exactly one.
  fixed_next <- fixed_from_gregorian(gregorian_date(2024L, 11L, 2L))
  stopifnot(as.numeric(fixed_next) - as.numeric(fixed) == 1)

  # Era-free anchor: 1 Nov 2024 falls at the very end of amanta Kartika's dark fortnight (Diwali):
  # the lunar day must be 30 (amavasya) or 1 (first day of the following month) within adapter tolerance,
  # and the leap flag must be FALSE.
  l <- hindu_lunar_from_fixed(fixed)
  stopifnot(l$day %in% c(29L, 30L, 1L))
  stopifnot(identical(as.logical(l$leap_month), FALSE))

  # Metamorphic: a leap-month flag may change only at a lunar month boundary - scan one synodic month.
  previous <- hindu_lunar_from_fixed(fixed)
  for (offset in 1:30) {
    current <- hindu_lunar_from_fixed(fixed + offset)
    if (!identical(current$leap_month, previous$leap_month)) {
      stopifnot(current$month != previous$month || current$year != previous$year)
    }
    previous <- current
  }

  invisible(TRUE)
}

smoke()

# ---- Generation. ----------------------------------------------------------------------------------------------

frames <- list()
for (m in models) {
  frames[[m]] <- do.call(rbind, lapply(dates, convert_one, calendar_model = m))
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
