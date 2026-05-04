# NightscoutFoundation.Nocturne.Model.CreateSystemEventRequest
Request body for creating a new SystemEvent record.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**EventType** | **SystemEventType** | Gets or sets the event type (alarm, warning, or info). | [optional] 
**Category** | **SystemEventCategory** | Gets or sets the event category. | [optional] 
**Code** | **string** | Gets or sets an optional short code identifying the event. | [optional] 
**Description** | **string** | Gets or sets a human-readable description of the event. | [optional] 
**Mills** | **long** | Gets or sets the Unix millisecond timestamp of the event. | [optional] 
**Source** | **string** | Gets or sets the data source identifier (defaults to \&quot;manual\&quot; when absent). | [optional] 
**Metadata** | **Dictionary&lt;string, Object&gt;** | Gets or sets arbitrary metadata associated with the event. | [optional] 
**OriginalId** | **string** | Gets or sets the original MongoDB ObjectId, preserved for migration compatibility. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

