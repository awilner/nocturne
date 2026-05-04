# NightscoutFoundation.Nocturne.Model.LinkedRecordsResponse
Response containing linked records for a canonical deduplication group.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**CanonicalId** | **string** | Gets or sets the canonical record identifier shared by all linked records. | [optional] 
**RecordType** | **RecordType** | Gets or sets the record type of the canonical group. | [optional] 
**LinkedRecords** | [**List&lt;LinkedRecord&gt;**](LinkedRecord.md) | Gets or sets the full list of records linked to the canonical group. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

