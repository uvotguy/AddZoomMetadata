# AddZoomMetadata

An example of how to add metadata to *Zoom* recordings.  *Kaltura* administrators can schedule this program as a daily task to add *Zoom* metadata to end users' recordings.

## Getting Started

This project was created in response to a request by Faculty and Staff at Penn State University.  Some users have a large number of *Kaltura* content and they were having trouble finding their course material.  Most faculty members use *Zoom* for remote course delivery, and the *Zoom* integration automatically uploads recordings to our *Kaltura* instance.  The integration software adds the recordings to a category called "Zoom Recordings".  The *Kaltura* search mechanism **does not** search the *category* field for entries, so there is no built-in way to search for them.

Here's what we did to help end users find their *Zoom* recordings.

We created a custom metadata schema to capture the following information:  Content Usage, Creative Commons License, and Open Educational Resources flag.  *Content Usage* is a multi-select field with items like "Personal Use", "Course Material", "Zoom Recording", and some others.  Users can select all that apply.  The schema sets each field as `<searchable>true</searchable>`, so a filter group is added to the MediaSpace "Filters" selection.

The next step is to add this metadata to all *Zoom* recordings in our repository.  The program, *AddZoomMetadata*, adds a "zoom" tag and "Zoom Recording" metadata field.  We ran the program against our entire repository, then we created a daily task to add metadata to newly created *Zoom* recordings.

End users can now easily find their *Zoom* recordings using the MediaSpace "Filter" mechanism.

### Prerequisites

This code is a *.Net Framework* Console application.  The following *NuGet* packages are required:

* **CommandLineParser** - The [CommandLineParser](https://github.com/commandlineparser/commandline) is a full featured, mature GitHub project which simplifies the task of parsing command line arguments.

* **NLog** - [NLog](https://nlog-project.org/) is also a full featured, mature product.  I use it for all my .Net software projects.  It has tons of features, and it just works!

* **Kaltura Client API for C#**

### Installing

Download the [Kaltura API Client for C#](https://developer.kaltura.com/api-docs/Client_Libraries/).  Compile the client, and run the tester.

Install the *CommandLineParser* and *NLog* *NuGet* packages.  Add a project reference to the *Kaltura Client API* (.dll), and compile the code with *Visual Studio*.

Check that the code runs.  In a command window, type:  `AddZoomMetadata.exe --help`

This should produce a list of command line options.

## Deployment

I typically copy the program files to *c:\Program Files\AddZoomMetadata*.

## Built With

* .Net Framework 4.x
* CommandLineParser
* NLog
* Kaltura API Client for C#

## Contributing

Please submit a pull request or issue.  I will respond as soon as possible.

## Versioning

See the code repository tags.

## Authors

See also the list of [contributors](https://github.com/uvotguy/AddZoomMetadata/contributors) who participated in this project.

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE.md](https://github.com/uvotguy/AddZoomMetadata/blob/master/LICENSE.md) file for details

## Acknowledgments

* **Billie Thompson** - *README Template* - [PurpleBooth](https://github.com/PurpleBooth)
* **Jack Sharon** - Thanks for giving me the nudge to share this code.
