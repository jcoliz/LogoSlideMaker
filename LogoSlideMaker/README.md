# Logo Slide Maker: Command-Line Tool

This project wraps the underlying layout & rendering engine into a simple
tool which can be run from the command line.

```dotnetcli
PS> dotnet run -- --help
LogoSlideMaker 1.0.0.0
  -h, --help                 show this message and exit
  -i, --input=toml           the toml containing slide definitions
  -o, --output=pptx          the pptx where the slides will be written
  -t, --template=template    the template to base the news slides on (optional)
  -l, --list                 also print a listing of logos
  -v, --version=version      add the specified version identifier to slide
                               notes (optional)
```
