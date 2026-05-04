# NightscoutFoundation.Nocturne.Model.PendingMigrationConfig
Auto-start migration configuration discovered from environment variables (MIGRATION_MODE, MIGRATION_NS_URL, MIGRATION_NS_API_SECRET, etc.). Credentials are never returned verbatim; only their presence is indicated via HasApiSecret and HasMongoConnectionString.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**HasPendingConfig** | **bool** | Whether there is a pending migration configuration in env vars | [optional] 
**Mode** | **MigrationMode** | Migration mode from MIGRATION_MODE env var | [optional] 
**NightscoutUrl** | **string** | Nightscout URL from MIGRATION_NS_URL env var | [optional] 
**HasApiSecret** | **bool** | Whether MIGRATION_NS_API_SECRET is set (never returns the actual secret) | [optional] 
**HasMongoConnectionString** | **bool** | Whether MIGRATION_MONGO_CONNECTION_STRING is set (never returns the actual string) | [optional] 
**MongoDatabaseName** | **string** | MongoDB database name from MIGRATION_MONGO_DATABASE_NAME env var | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

