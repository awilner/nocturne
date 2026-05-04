# NightscoutFoundation.Nocturne.Model.CreateTrackerDefinitionRequest

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Name** | **string** |  | 
**Description** | **string** |  | [optional] 
**Category** | **TrackerCategory** |  | [optional] 
**Icon** | **string** |  | [optional] 
**TriggerEventTypes** | **List&lt;string&gt;** |  | [optional] 
**TriggerNotesContains** | **string** |  | [optional] 
**LifespanHours** | **int?** |  | [optional] 
**NotificationThresholds** | [**List&lt;CreateNotificationThresholdRequest&gt;**](CreateNotificationThresholdRequest.md) |  | [optional] 
**IsFavorite** | **bool** |  | [optional] 
**DashboardVisibility** | **DashboardVisibility** | Dashboard visibility: Off, Always, Info, Warn, Hazard, Urgent | [optional] 
**Visibility** | **TrackerVisibility** | Visibility level for this tracker (Public, Private, RoleRestricted) | [optional] 
**StartEventType** | **string** | Event type to create when tracker is started (for Nightscout compatibility) | [optional] 
**CompletionEventType** | **string** | Event type to create when tracker is completed (for Nightscout compatibility) | [optional] 
**Mode** | **TrackerMode** | Tracker mode: Duration (time-based) or Event (scheduled datetime) | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

