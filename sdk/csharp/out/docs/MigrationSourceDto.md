# NightscoutFoundation.Nocturne.Model.MigrationSourceDto
Migration source DTO for API responses

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **string** | Unique identifier for this source | [optional] 
**Mode** | **MigrationMode** | Migration mode (Api or MongoDb) | [optional] 
**NightscoutUrl** | **string** | Nightscout URL (for API mode) | [optional] 
**MongoDatabaseName** | **string** | MongoDB database name (for MongoDB mode) | [optional] 
**LastMigrationAt** | **DateTimeOffset?** | When the last successful migration completed | [optional] 
**LastMigratedDataTimestamp** | **DateTimeOffset?** | Newest data timestamp migrated (for \&quot;since last\&quot; default) | [optional] 
**CreatedAt** | **DateTimeOffset** | When this source was first added | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

