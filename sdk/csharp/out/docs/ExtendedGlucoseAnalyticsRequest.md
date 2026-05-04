# NightscoutFoundation.Nocturne.Model.ExtendedGlucoseAnalyticsRequest
Request model for extended glucose analytics with GMI, GRI, and clinical assessment

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Entries** | [**List&lt;SensorGlucose&gt;**](SensorGlucose.md) | Collection of sensor glucose readings | [optional] 
**Boluses** | [**List&lt;Bolus&gt;**](Bolus.md) | Optional collection of bolus deliveries | [optional] 
**CarbIntakes** | [**List&lt;CarbIntake&gt;**](CarbIntake.md) | Optional collection of carb intakes | [optional] 
**Population** | **DiabetesPopulation** | Diabetes population type for clinical target assessment | [optional] 
**Config** | [**ExtendedAnalysisConfig**](ExtendedAnalysisConfig.md) | Optional extended analysis configuration | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

