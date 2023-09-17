# DBFeeder - EF Core library

This library contains all necessary components to communicate with the destination database using Entity Framework Core.
The approach is model-first, using Entity Developer it is possible to create a model containing the entities/tables that will need to be populated with the information extracted through the Scraper and Crawler services.
A connection string needs to be set in the resx file present in Properties folder.

## Creation of a model

Just opening the SampleDataModel.efml file present in this folder and modyfing the diagram with the entities needed all it is necessary is to Generate the code (still in Entity Developer) and generate the SQL script to create the tables.
The entities will be generated in Entities folder while the context in the Context folder.

