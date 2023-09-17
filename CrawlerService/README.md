# DBFeeder - Crawler

Documentation in progress...

This is the first service to be instantiated and starting to extract urls to scrap given an initial url.
It fetches the HTML page of the initial url (urlBase/urlSuffix) and Navigate through the page in order to find the Target (a string representing an URL).
For each target/url it will create an AMQPUrlMessage object that will be sent to the Event Bus.


## Design

The Crawler works as multithread, a new background thread for each configuration file present in the configs folder.

