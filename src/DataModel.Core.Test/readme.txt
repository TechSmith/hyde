Project goals:

Abstract table storage away to allow:
* POCO C# objects without knowledge of PK, RK, TS, or TableServiceEntity.
* Repository objects without knowledge of TableServiceContext.
* Data stored in table columns with a one-to-one mapping with properties on the POCO objects.
* In memory store needs to be able to simulate error conditions.
* Unity shouldn't be required in the Core libraries.
* Continuation tokens should be hidden.

TODO:
* Need to support the limited filters that table storage can handle (like timestamp).
* Support complex types that are ISerializable

Errors to deal with:
*** The entity already exists *** 
Test method TechSmith.CloudServices.Table.DataModel.CoreIntegrationTests.AzureTableContextTests.AddItem_ValidItemToAddToStore_ItemAddedToStore threw exception: 
System.Data.Services.Client.DataServiceRequestException: An error occurred while processing this request. ---> System.Data.Services.Client.DataServiceClientException: <?xml version="1.0" encoding="utf-8" standalone="yes"?>
<error xmlns="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata">
  <code>EntityAlreadyExists</code>
  <message xml:lang="en-US">The specified entity already exists.</message>
</error>

