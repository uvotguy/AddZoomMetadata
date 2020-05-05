# Using the AddZoomMetadata Utility

With the AddZoomMetadata utilitiy compiled (see the README file), get a list of command line options like this:

`AddZoomMetadata.exe --help`

## Initialization

The utilty does two things:  add a *zoom* tag to targeted entries and add or modify custom metadata to them.  The *Kaltura API Client* can access the data in your *Kaltura* instance, but it requires some setup.  The client connection is established using your *Kaltura Partner ID* and *Admin Secret*.  To avoid requiring you to enter them each time you run the code, *AddZoomMetadata* stores that information in your user profile (see %HOMEDIR%\AppData\Local\AddZoomMetadata\).

Seed your config file by processing a non-existant entry:

`AddZoomMetadata.exe --generate-xml false --partner-id {your PID} --api-secret {admin secret} --entry-id abc`

Verify initialization worked:

`AddZoomMetadata.exe --generate-xml false --entry-id abc`

The code should run to completion without an API exception.

### Targeting Media

*AddZoomMetadata* is configured to select entries in the *Zoom Recordings* category.  If you choose to target entries in a different category, you will need to make that change in the C# code.

### Tags

*AddZoomMetadata* adds a *zoom* tag to the targeted entries (if one does not already exist).  To change the tag, modify the C# code.

### Metadata

Adding metadata to your targeted entries can simplify searching in *MediaSpace*.  Find your target metadata schema in the KMC:  *KMC -> Settings -> Custom Data*.  Fields configured as *<searchable>true</searchable>* are added to the search filter in *MediaSpace*.  Our metadata field looks somethinglike this:
```
<metadata>
    <MediaType>Zoom Recording</MediaType>
</metadata>
```

You will need to modify the *KalturaUtilities -> addCustomMetadata()* method to suit your needs.  Keep in mind that **XML fields are order-dependent**.  Be sure to insert fields in the order defined in your schema.

## Generate XML Mode

If you decide to add metadata to all *Zoom* recordings in your instance, you will probably want to do it via the Bulk Upload method.  Bulk Upload runs many times faster than in-line API calls.  Use *--generate-xml true* to create Bulk Upload XML files, then upload the files via the standard Bulk Upload interface in the KMC.  Our instance had 200k *Zoom* recordings, and it took almost a week to add a *zoom* tags using API calls.  Adding custom metadata via Bulk Upload took only 12 hours!

The **--generate-xml** option is required; either *--generate-xml true* or *--generate-xml false* must be specified.  When in "generate XML" mode, the output is one or more XML files suitable for bulk upload processing.  The number of files generated depends upon the number of media entries to be processed.  The default page size returned by each API query is 500 items.

When processing is complete, simply submit the XML files via the Bulk Upload interface in the KMC.

## API Mode

After initially processing your media catalog, you will probably want to set up a scheduled task to add metadata to new entries daily.  To do that, use *--generate-xml false*.  The proper API calls will be made to add metadata "on the fly".

## Single Entry Mode

To process a single media entry use the **--entry-id {id}** option.  The program will add metadata to the given Entry ID.  It will **not** store the timestamp of the media entry.

## Multiple Entries Mode

In Multiple Entries Mode the program processes all *Zoom* recordings created since the last time the program was run.  The program processes entries in order of their creation; it stores the timestamp of the last media entry.  See the *startTimestamp* field in your user config file.

Single Entry Mode and Multiple Entries Mode are mutually exclusive.

It is possible to start processing at an arbitrary date using the *--start-date* option like this:

`AddZoomMetadata.exe --generate-xml false --start-date 2020-01-01`

Any valid date format should be parsed properly.

## Logging

The *NLog* package is used for logging.  The default behavior writes per-date log file in *c:\Logs\Nlog\AddZoomMetadata\{yyyy-mm-dd}.log*.  Modify *Nlog.config* to suit your purposes.