// Bodu overlay on the DocFX ManagedReference preprocessor.
//
// The API metadata this template renders has been through bld/docs/merge_framework_metadata.py, which
// adds three things DocFX does not produce itself:
//
//   frameworks  on every item and on references to documented APIs: the target frameworks the API
//               exists in, oldest first ("net8.0", "net10.0");
//   package     on every type: { id, version, published } for the NuGet package it ships in;
//   packages    on every namespace: the distinct packages of the types it contains.
//
// _boduFrameworks (global metadata from docs/obj/api/frameworks.json) lists every documented framework,
// newest first, with the names a reader sees ({ tfm, product, version, label, view }).
//
// postTransform turns those into the flat, render-ready values the bodu partials read. Every value is
// set on every child explicitly, even when empty: Mustache resolves a key a child lacks from the
// enclosing type, so a member without its own value would silently show its type's.
//
// Plain ES5 on purpose: DocFX runs this in its embedded JavaScript engine, not a browser.

exports.preTransform = function (model) {
  return model;
};

exports.postTransform = function (model) {
  var catalog = frameworkCatalog(model._boduFrameworks);
  var pageFrameworks = model.frameworks || [];

  describeFrameworks(model, catalog, null);
  model.boduFrameworkList = catalog.list.map(function (f) { return f.tfm + '|' + f.label + '|' + f.view; }).join(';');
  model.boduHasFrameworkState = pageFrameworks.length > 0 && catalog.list.length > 0;
  model.boduAppliesTo = appliesTo(pageFrameworks, catalog);

  describePackage(model, model.package, model._boduNuGetPackageUrl);
  model.boduPackages = (model.packages || []).map(function (p) {
    var row = {};
    describePackage(row, p, model._boduNuGetPackageUrl);
    return row;
  });
  model.boduPackagesLabel = model.boduPackages.length > 1 ? 'Packages' : 'Package';
  model.boduSourceFile = sourceFileName(model.sourceurl);

  // Class and namespace pages group their children by kind (constructors, methods, …; classes,
  // enums, …) before this runs, so the members are one level further down.
  (model.children || []).forEach(function (group) {
    (group.children || [group]).forEach(function (child) {
      describeFrameworks(child, catalog, pageFrameworks);
    });
  });

  return model;
};

function frameworkCatalog(frameworks) {
  var list = frameworks || [];
  var byTfm = {};
  list.forEach(function (f) { byTfm[f.tfm] = f; });
  return { list: list, byTfm: byTfm };
}

// Sets the selector attribute and, when the item's frameworks differ from the page's, an availability
// note such as ".NET 10 only".
function describeFrameworks(item, catalog, pageFrameworks) {
  var frameworks = item.frameworks || [];
  item.boduFrameworksAttr = frameworks.join(' ');
  item.boduAvailability = '';
  if (pageFrameworks && frameworks.length > 0 && frameworks.join(' ') !== pageFrameworks.join(' ')) {
    item.boduAvailability = frameworks.map(function (tfm) { return label(catalog, tfm); }).join(', ') + ' only';
  }
}

function label(catalog, tfm) {
  var f = catalog.byTfm[tfm];
  return f ? f.label : tfm;
}

// Groups the item's frameworks by product, versions in ascending order: [{ product: ".NET", versions: "8, 10" }].
function appliesTo(frameworks, catalog) {
  var rows = [];
  var byProduct = {};
  frameworks.forEach(function (tfm) {
    var f = catalog.byTfm[tfm] || { product: tfm, version: '' };
    if (!byProduct[f.product]) {
      byProduct[f.product] = { product: f.product, versions: [] };
      rows.push(byProduct[f.product]);
    }
    if (f.version) byProduct[f.product].versions.push(f.version);
  });
  return rows.map(function (row) { return { product: row.product, versions: row.versions.join(', ') }; });
}

function describePackage(target, pkg, nugetBaseUrl) {
  target.boduPackageId = pkg ? pkg.id : '';
  target.boduPackageVersion = pkg ? pkg.version : '';
  target.boduPackagePublished = !!(pkg && pkg.published);
  // Only a package on nuget.org gets a link, and it names the package rather than this version: the
  // /dev/ site documents versions that are not released yet. The version stays plain text beside it.
  target.boduPackageUrl = pkg && pkg.published && nugetBaseUrl ? nugetBaseUrl + encodeURIComponent(pkg.id) : '';
}

// "https://github.com/o/r/blob/<sha>/Bodu.Core/src/Buffers/PooledBufferBuilder%7BT%7D.cs#L10" -> "PooledBufferBuilder{T}.cs"
function sourceFileName(url) {
  if (!url) return '';
  var path = url.split('#')[0].split('?')[0];
  var name = path.substring(path.lastIndexOf('/') + 1);
  try {
    return decodeURIComponent(name);
  } catch (e) {
    return name;
  }
}
