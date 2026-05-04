# NightscoutFoundation.Nocturne.Model.UpsertBGCheckRequest
Request body for upserting a blood glucose (BG) check record via the V4 API.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Timestamp** | **DateTimeOffset** | When the BG check was performed. | [optional] 
**UtcOffset** | **int?** | UTC offset in minutes at the time of the event, for local-time display. | [optional] 
**Device** | **string** | Identifier of the device that performed the check. | [optional] 
**App** | **string** | Name of the application that submitted this record. | [optional] 
**DataSource** | **string** | Upstream data source identifier. | [optional] 
**Glucose** | **double** | Blood glucose reading value (validated 0-10,000). | [optional] 
**Units** | **GlucoseUnit** | Unit of the glucose reading (mg/dL or mmol/L). | [optional] 
**GlucoseType** | **GlucoseType** | Origin of the glucose value (finger stick, sensor, manual, etc.). | [optional] 
**SyncIdentifier** | **string** | Upstream sync identifier for deduplication. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

