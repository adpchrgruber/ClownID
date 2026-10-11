# Changelog

## 0.9.1-beta — 2026-10-11

- **ClownID toolbar**: both commands as buttons, in a ClownID tab next to Rhino's other
  toolbars. Installed with the package.

## 0.9.0-beta — 2026-10-11

First public beta of **ClownID**, a Rhino 8 plug-in that shows the clown pass (object-ID
mask) live in the viewport and exports it as a clean mask for compositing.

### What's in it

- **Clown ID display mode**: every object in one flat, unlit colour derived from its id,
  on a black background, without grid, curves, edges or annotations. Use it in any
  viewport, for example next to a rendered one.
- **`ClownID`**: switches the active viewport to Clown ID and back.
- **`ClownIDCapture`**: saves the view as a PNG plus a JSON colour key (colour → object
  id, name, layer, type, pixel count). Set *Width* and *Height* to your render size and
  the mask lines up with the render pixel for pixel.
- **Stable colours**: the same object gets the same colour in every session and every
  file. Block instances get one colour each.
- **Exact colours**: no lighting, gamma or tone mapping. Anti-aliased edges are snapped to
  a neighbouring object's colour, so every pixel is exactly one object or the background.

### Install

1. Download `clownid-0.9.0-beta-rh8_0-any.yak`.
2. Drag it into a Rhino 8 window, then restart Rhino.
3. Run `ClownID`.

Requires Rhino 8. Tested on Rhino 8.35 for Mac; Windows is untested so far.

### Known limitations

- Colours come from a hash, so two objects can, rarely, share one. `ClownIDCapture`
  reports it and lists them under `collisions` in the JSON.
- Grasshopper previews are not document objects and draw normally. Bake them first.
- Only objects with render meshes (surfaces, polysurfaces, meshes, and blocks of them)
  appear in the pass. Curves, points and annotations are left out by design.
