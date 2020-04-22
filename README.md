# AddZoomMetadata

An example of how to add metadata to Zoom recordings.

## Getting Started

This project was created in response to a request by Faculty and Staff at Penn State University.  Some users have a large number of Kaltura content and they were having trouble finding their course material.  Most faculty members use Zoom for remote course delivery.

Our Kaltura instance has a Zoom integration.  The Zoom module automatically adds Zoom recordings to a category called "Zoom Recordings".  The Kaltura search mechanism does not search the category field for entries, so there is no built-in way to search for them.

Here's what we did.  We created a custom metadata schema end users can use to flag their content in several ways:  Content Usage, Creative Commons license, and Open Educational Resources flag.  Content Usage is a multi-select for "Personal Use",
"Course Material", "Zoom Recording", and some others.  This custom schema marks each field as <searchable>true</searchable>.  Making the fields searchable causes the MediaSpace interface to add these fields to the "Filters" selection.

The next step is to add this metadata to all Zoom recordings in our repository.  The program, AddZoomMetadata, is custom software we created to find zoom recordings and add the following metadata:  a "zoom" tag and "Zoom Recording" metadata field.  We ran the program against our entire repository.

With the metadata created, end users can easily find their Zoom recordings using the MediaSpace "Filter" mechanism.

### Prerequisites

This code is a .Net Framework Console application.  The following NuGet packages are required:

* **CommandLineParser** - The CommandLineParser is a full featured, mature GitHub project which makes command line parsing much easier.  See https://github.com/commandlineparser/commandline.

* **NLog** - NLog is also a full featured, mature product.  I use it for all my .Net software projects.  It has tons of features, and it just works!!!  The project website is https://nlog-project.org/.

* **Kaltura Client API for C#** - You will need to download the latest client and compile it.  Be sure to add a reference to the .dll.

### Installing

Download the Kaltura API Client for C#:  https://developer.kaltura.com/api-docs/Client_Libraries/.  Compile the client.

Install the CommandLineParser and NLog NuGet packages.  Add a link to the Kaltura Client library .dll, and compile the code with Visual Studio.

Check that the code runs.  In a command window, type:  AddZoomMetadata.exe --help

This should produce a list of command line options.

## Deployment

I typically copy the program files to c:\Program Files\AddZoomMetadata.

## Built With

* .Net Framework 4.x
* CommandLineParser
* NLog
* Kaltura API Client for C#

## Contributing

Please read [CONTRIBUTING.md](https://gist.github.com/PurpleBooth/b24679402957c63ec426) for details on our code of conduct, and the process for submitting pull requests to us.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## Authors

See also the list of [contributors](https://github.com/your/project/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* **Billie Thompson** - *Initial work* - [PurpleBooth](https://github.com/PurpleBooth)
* **Jack Sharon** - Thanks for giving me the nudge to share this code.
