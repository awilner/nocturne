# NightscoutFoundation.Nocturne.Model.CreateMealRequest
Request body for creating a correlated meal event (a single bolus + single carb intake sharing a CorrelationId, created atomically).

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Timestamp** | **DateTimeOffset** | When the meal occurred. | [optional] 
**UtcOffset** | **int?** | UTC offset in minutes at the time of the event, for local-time display. | [optional] 
**Insulin** | **double** | Total insulin amount in units for the meal bolus. | [optional] 
**Carbs** | **double** | Amount of carbohydrates consumed in grams. | [optional] 
**BolusType** | **BolusType** | Bolus delivery pattern (normal, square wave, dual wave, etc.). | [optional] 
**Duration** | **double?** | Extended/square bolus duration in minutes. | [optional] 
**AbsorptionTime** | **int?** | Expected carb absorption duration in minutes. | [optional] 
**CarbTime** | **double?** | Minutes from bolus time to expected carb absorption start (pre-bolus offset). | [optional] 
**InsulinType** | **string** | Type or brand of insulin used (e.g. \&quot;Humalog\&quot;, \&quot;NovoRapid\&quot;). | [optional] 
**Device** | **string** | Identifier of the device that delivered the bolus. | [optional] 
**App** | **string** | Name of the application that submitted this record. | [optional] 
**DataSource** | **string** | Upstream data source identifier; required when SyncIdentifier is supplied. | [optional] 
**SyncIdentifier** | **string** | Upstream sync identifier for deduplication, paired with DataSource. | [optional] 
**BolusCalculationId** | **string** | Links this meal to the bolus calculation that recommended the insulin dose. | [optional] 
**CorrelationId** | **string** | Caller-supplied correlation identifier; if omitted, the server generates one. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

