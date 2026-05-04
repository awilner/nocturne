# NightscoutFoundation.Nocturne.Model.UpsertDeviceEventRequest
Request body for upserting a device event record (site changes, sensor starts, etc.) via the V4 API.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Timestamp** | **DateTimeOffset** | When the device event occurred. | [optional] 
**UtcOffset** | **int?** | UTC offset in minutes at the time of the event, for local-time display. | [optional] 
**Device** | **string** | Identifier of the device involved in the event. | [optional] 
**App** | **string** | Name of the application that submitted this record. | [optional] 
**DataSource** | **string** | Upstream data source identifier. | [optional] 
**EventType** | **DeviceEventType** | The type of device event (e.g. site change, sensor start, pump resume). | [optional] 
**Notes** | **string** | Free-text notes associated with the event (capped at 10,000 characters). | [optional] 
**SyncIdentifier** | **string** | Upstream sync identifier for deduplication. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

