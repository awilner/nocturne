# NightscoutFoundation.Nocturne.Model.TenantSnapshotDto
Snapshot of a single tenant's identity and configuration data for dev-only export/import.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Tenant** | [**TenantEntityDto**](TenantEntityDto.md) | The tenant entity itself. | [optional] 
**Subjects** | [**List&lt;SubjectEntityDto&gt;**](SubjectEntityDto.md) | All subjects (users/service accounts) belonging to this tenant. | [optional] 
**PasskeyCredentials** | [**List&lt;PasskeyCredentialEntityDto&gt;**](PasskeyCredentialEntityDto.md) | All passkey credentials registered for this tenant&#39;s subjects. | [optional] 
**Roles** | [**List&lt;TenantRoleEntityDto&gt;**](TenantRoleEntityDto.md) | All tenant-scoped roles. | [optional] 
**Members** | [**List&lt;TenantMemberEntityDto&gt;**](TenantMemberEntityDto.md) | All tenant membership records linking subjects to the tenant. | [optional] 
**MemberRoles** | [**List&lt;TenantMemberRoleEntityDto&gt;**](TenantMemberRoleEntityDto.md) | All role assignments for tenant members. | [optional] 
**OAuthClients** | [**List&lt;OAuthClientEntityDto&gt;**](OAuthClientEntityDto.md) | All OAuth client registrations for this tenant. | [optional] 
**ConnectorConfigurations** | [**List&lt;ConnectorConfigSnapshotDto&gt;**](ConnectorConfigSnapshotDto.md) | All connector configurations (with secrets decrypted) for this tenant. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

