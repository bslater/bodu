#!/usr/bin/env Rscript
# ---------------------------------------------------------------------------------------------------------------
# verify_environment.R - pins the calcal Modern Hindu Reference environment before corpus generation.
#
# A version number alone is insufficient where a package can be rebuilt or re-released: also record and verify
# the package archive SHA-256 (or the Git commit) in renv.lock / the generation log.
# ---------------------------------------------------------------------------------------------------------------

required_version <- package_version("1.0.4")
actual_version <- utils::packageVersion("calcal")

if (actual_version != required_version) {
  stop(sprintf(
    "calcal version mismatch: expected %s, found %s",
    required_version,
    actual_version
  ))
}

cat(sprintf("R=%s\n", R.version.string))
cat(sprintf("calcal=%s\n", actual_version))
cat(sprintf("calcal-path=%s\n", find.package("calcal")))

description <- packageDescription("calcal")
for (field in c("Packaged", "Built", "RemoteSha", "Repository")) {
  if (!is.null(description[[field]])) {
    cat(sprintf("%s=%s\n", field, description[[field]]))
  }
}
