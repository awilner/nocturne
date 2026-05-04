# NightscoutFoundation.Nocturne.Model.GlucosePredictionResponse
Response containing glucose predictions.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Timestamp** | **DateTimeOffset** | Timestamp when predictions were calculated | [optional] 
**CurrentBg** | **double** | Current blood glucose (mg/dL) | [optional] 
**Delta** | **double** | Rate of glucose change (mg/dL per 5 min) | [optional] 
**EventualBg** | **double** | Eventual blood glucose if trend continues (mg/dL) | [optional] 
**Iob** | **double** | Current insulin on board (U) | [optional] 
**Cob** | **double** | Current carbs on board (g) | [optional] 
**SensitivityRatio** | **double?** | Sensitivity ratio used (1.0 &#x3D; normal) | [optional] 
**IntervalMinutes** | **int** | Prediction interval in minutes | [optional] 
**Predictions** | [**PredictionCurves**](PredictionCurves.md) | Prediction curves with different scenarios | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

