# NightscoutFoundation.Nocturne.Model.UploaderSetupResponse
Response model for uploader setup instructions

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**App** | [**UploaderApp**](UploaderApp.md) | The uploader app details | [optional] 
**BaseUrl** | **string** | Base URL for this Nocturne instance | [optional] 
**ConnectUrl** | **string** | Deep-link URL for apps that support OAuth device authorization via QR code (e.g. xdrip://connect/nocturne?url&#x3D;https://your-instance.com). When present, the frontend shows a QR code and inline device-code input. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

