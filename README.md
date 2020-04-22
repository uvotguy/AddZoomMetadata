# AddZoomMetadata
An example of how to add metadata to Zoom recordings.

The Problem

This project was created in response to a request by Faculty and Staff at Penn State
University.  Some users have a large number of Kaltura content and they were having
trouble finding their course material.  Most faculty members use Zoom for remote
course delivery.

Our Kaltura instance has a Zoom integration.  The Zoom module automatically adds Zoom
recordings to a category called "Zoom Recordings".  The Kaltura search mechanism does
not search the category field for entries, so there is no built-in way to search for
them.

Here's what we did.  We created a custom metadata schema end users can use to flag
their content in several ways:  Content Usage, Creative Commons license, and Open
Educational Resources flag.  Content Usage is a multi-select for "Personal Use",
"Course Material", "Zoom Recording", and some others.  This custom schema marks each
field as <searchable>true</searchable>.  Making the fields searchable causes the
MediaSpace interface to add these fields to the "Filters" selection.

The next step is to add this metadata to all Zoom recordings in our repository.  The
program, AddZoomMetadata, is custom software we created to find zoom recordings and
add the following metadata:  a "zoom" tag and "Zoom Recording" metadata field.  We
ran the program against our entire repository.

With the metadata created, end users can easily find their Zoom recordings using the
MediaSpace "Filter" mechanism.

