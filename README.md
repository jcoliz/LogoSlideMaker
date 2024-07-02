# Logo Slide Maker

![Logo Slide Maker](./docs/images/LogoSlideMakerScreenshot.png)

Do you need a PowerPoint slide containing a gallery of logos? Perhaps you need to brag how many
other projects consume your technology? Or show off how many projects you're compatible with?
Or how many disparate technology components you've leverage in your own solution?

Logo Slide Maker is a Windows Desktop application to build, preview, and generate PowerPoint presentations containing
logos with text. It can also be run from a terminal window. 

## Getting Started

Here's the basic getting started guide:

1. Create a TOML file describing your logos. Feel free to start with the [sample](./sample/sample.toml)!
1. Open the TOML file in the app to preview.
1. Make changes to the TOML as needed to fulfil your vision, and reload to update the preview with your changes.
1. If you have multiple slides defined, view each slide individually, using Previous and Next commands. 
1. Once you're happy, export the slides to a PowerPoint presentation

## Unfinished Business

The app is extremely useful as-is, however it's not totally complete yet. Of note:

* Errors are not surfaced to the user. This is why a log window pops up. Have a look at the log window if things aren't quite working right
* Only source is available. For now, the only way to use the tool is to clone this repo and build it yourself. In the future, fully built versions will be available for download here, and ultimately it will show up in the Microsoft Store.

For more details, please see the [Issues](https://github.com/jcoliz/LogoSlideMaker/issues) section of this repo for a full list of functionality envisioned for the future.
