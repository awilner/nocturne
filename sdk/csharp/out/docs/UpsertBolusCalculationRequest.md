# NightscoutFoundation.Nocturne.Model.UpsertBolusCalculationRequest
Request body for upserting a bolus calculator wizard record via the V4 API. Captures the inputs and recommendation from a bolus calculation event.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Timestamp** | **DateTimeOffset** | When the bolus calculation was performed. | [optional] 
**UtcOffset** | **int?** | UTC offset in minutes at the time of the event, for local-time display. | [optional] 
**Device** | **string** | Identifier of the device that ran the calculation. | [optional] 
**App** | **string** | Name of the application that submitted this record. | [optional] 
**DataSource** | **string** | Upstream data source identifier. | [optional] 
**BloodGlucoseInput** | **double?** | Blood glucose value used as input to the calculator. | [optional] 
**BloodGlucoseInputSource** | **string** | Source of the BG input (e.g. \&quot;CGM\&quot;, \&quot;Manual\&quot;, \&quot;Meter\&quot;). | [optional] 
**CarbInput** | **double?** | Carbohydrate amount (grams) used as input to the calculator. | [optional] 
**InsulinOnBoard** | **double?** | Insulin on board at the time of calculation, in units. | [optional] 
**InsulinRecommendation** | **double?** | Total insulin dose recommended by the calculator, in units. | [optional] 
**CarbRatio** | **double?** | Insulin-to-carb ratio used in the calculation (grams per unit). Must be strictly positive. | [optional] 
**CalculationType** | **CalculationType** | Type of calculation performed (e.g. correction only, meal only, combined). | [optional] 
**InsulinRecommendationForCarbs** | **double?** | Portion of the recommendation attributable to carb coverage. | [optional] 
**InsulinProgrammed** | **double?** | Insulin amount actually programmed into the pump. | [optional] 
**EnteredInsulin** | **double?** | Insulin amount manually entered by the user. | [optional] 
**SplitNow** | **double?** | Percentage of a dual-wave bolus delivered immediately. | [optional] 
**SplitExt** | **double?** | Percentage of a dual-wave bolus delivered as extended. | [optional] 
**PreBolus** | **double?** | Pre-bolus time in minutes (insulin given before eating). | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

