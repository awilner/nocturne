# NightscoutFoundation.Nocturne.Model.StartMigrationRequest
Parameters for starting a new data migration from Nightscout or MongoDB.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Mode** | **MigrationMode** | Migration mode (API or MongoDB) | [optional] 
**NightscoutUrl** | **string** | Nightscout URL (for API mode) | [optional] 
**NightscoutApiSecret** | **string** | Nightscout API secret (for API mode) | [optional] 
**MongoConnectionString** | **string** | MongoDB connection string (for MongoDB mode) | [optional] 
**MongoDatabaseName** | **string** | MongoDB database name (for MongoDB mode) | [optional] 
**Collections** | **List&lt;string&gt;** | Collections to migrate. Empty means all. | [optional] 
**StartDate** | **DateTimeOffset?** | Start date for migration (optional) | [optional] 
**EndDate** | **DateTimeOffset?** | End date for migration (optional) | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

