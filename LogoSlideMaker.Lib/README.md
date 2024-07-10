# Logo Slide Maker: Layout & rendering engine

This project encapsulates the platform-independent layout and rendering logic.

## Pipeline

Stages from load to render:

1. CONFIGURE. In this stage, a `Definition` is built. Typically, we deserialize this from a TOML file, but this is not required.
1. LOAD/MEASURE. Here, we load all the logo files specified in the definition, and measure them. This phase is coupled to the renderer which will ultimately render them.
1. LAYOUT. For any given slide, we can create a `SlideLayout`. This determines which logos should be included, and where they should be placed
1. PRIMITIVES. For any given slide, we create drawing primitives for a layout. These primitives are renderer-agnostic, so they could be rendered by the preview renderer or the PowerPoint renderer.
1. RENDER. Using the device-independent primitives, display them in the correct way for the user. The preview renderer shows them on a Win2d canvas. The export renderer commits them to a PowerPoint slide.
