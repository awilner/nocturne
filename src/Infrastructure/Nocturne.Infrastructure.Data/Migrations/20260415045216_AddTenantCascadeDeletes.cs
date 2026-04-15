using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nocturne.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_uploader_snapshots_tenant_id",
                table: "uploader_snapshots",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_treatments_tenant_id",
                table: "treatments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_foods_tenant_id",
                table: "treatment_foods",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tracker_presets_tenant_id",
                table: "tracker_presets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tracker_notification_thresholds_tenant_id",
                table: "tracker_notification_thresholds",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tracker_instances_tenant_id",
                table: "tracker_instances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tracker_definitions_tenant_id",
                table: "tracker_definitions",
                column: "tenant_id");

            // IX_system_events_tenant_id may already exist from AddTenantIdToSystemEvents migration
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_system_events_tenant_id" ON system_events (tenant_id);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_step_counts_tenant_id",
                table: "step_counts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_state_spans_tenant_id",
                table: "state_spans",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_pump_snapshots_tenant_id",
                table: "pump_snapshots",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_tenant_id",
                table: "profiles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_device_codes_tenant_id",
                table: "oauth_device_codes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_authorization_codes_tenant_id",
                table: "oauth_authorization_codes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_glucose_tenant_id",
                table: "meter_glucose",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_in_app_notifications_tenant_id",
                table: "in_app_notifications",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_heart_rates_tenant_id",
                table: "heart_rates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_entries_tenant_id",
                table: "entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_discrepancy_details_tenant_id",
                table: "discrepancy_details",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_discrepancy_analyses_tenant_id",
                table: "discrepancy_analyses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_devicestatus_tenant_id",
                table: "devicestatus",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_devices_tenant_id",
                table: "devices",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_connector_configurations_tenant_id",
                table: "connector_configurations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_compression_low_suggestions_tenant_id",
                table: "compression_low_suggestions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_clock_faces_tenant_id",
                table: "clock_faces",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_tenant_id",
                table: "calibrations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_body_weights_tenant_id",
                table: "body_weights",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_aps_snapshots_tenant_id",
                table: "aps_snapshots",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_tracker_state_tenant_id",
                table: "alert_tracker_state",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_step_channels_tenant_id",
                table: "alert_step_channels",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_schedules_tenant_id",
                table: "alert_schedules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_rules_tenant_id",
                table: "alert_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_invites_tenant_id",
                table: "alert_invites",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_instances_tenant_id",
                table: "alert_instances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_escalation_steps_tenant_id",
                table: "alert_escalation_steps",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_deliveries_tenant_id",
                table: "alert_deliveries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_custom_sounds_tenant_id",
                table: "alert_custom_sounds",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_tenant_id",
                table: "activities",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_activities_tenants_tenant_id",
                table: "activities",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_custom_sounds_tenants_tenant_id",
                table: "alert_custom_sounds",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_deliveries_tenants_tenant_id",
                table: "alert_deliveries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_escalation_steps_tenants_tenant_id",
                table: "alert_escalation_steps",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_excursions_tenants_tenant_id",
                table: "alert_excursions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_instances_tenants_tenant_id",
                table: "alert_instances",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_invites_tenants_tenant_id",
                table: "alert_invites",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_rules_tenants_tenant_id",
                table: "alert_rules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_schedules_tenants_tenant_id",
                table: "alert_schedules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_step_channels_tenants_tenant_id",
                table: "alert_step_channels",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_alert_tracker_state_tenants_tenant_id",
                table: "alert_tracker_state",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_aps_snapshots_tenants_tenant_id",
                table: "aps_snapshots",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_basal_schedules_tenants_tenant_id",
                table: "basal_schedules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bg_checks_tenants_tenant_id",
                table: "bg_checks",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_body_weights_tenants_tenant_id",
                table: "body_weights",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bolus_calculations_tenants_tenant_id",
                table: "bolus_calculations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_boluses_tenants_tenant_id",
                table: "boluses",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_calibrations_tenants_tenant_id",
                table: "calibrations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_carb_intakes_tenants_tenant_id",
                table: "carb_intakes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_carb_ratio_schedules_tenants_tenant_id",
                table: "carb_ratio_schedules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_clock_faces_tenants_tenant_id",
                table: "clock_faces",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_compression_low_suggestions_tenants_tenant_id",
                table: "compression_low_suggestions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_connector_configurations_tenants_tenant_id",
                table: "connector_configurations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_connector_food_entries_tenants_tenant_id",
                table: "connector_food_entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_data_source_metadata_tenants_tenant_id",
                table: "data_source_metadata",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_device_events_tenants_tenant_id",
                table: "device_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_devices_tenants_tenant_id",
                table: "devices",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_devicestatus_tenants_tenant_id",
                table: "devicestatus",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_discrepancy_analyses_tenants_tenant_id",
                table: "discrepancy_analyses",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_discrepancy_details_tenants_tenant_id",
                table: "discrepancy_details",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_entries_tenants_tenant_id",
                table: "entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_foods_tenants_tenant_id",
                table: "foods",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_heart_rates_tenants_tenant_id",
                table: "heart_rates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_in_app_notifications_tenants_tenant_id",
                table: "in_app_notifications",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linked_records_tenants_tenant_id",
                table: "linked_records",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_meter_glucose_tenants_tenant_id",
                table: "meter_glucose",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_tenants_tenant_id",
                table: "notes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_authorization_codes_tenants_tenant_id",
                table: "oauth_authorization_codes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_clients_tenants_tenant_id",
                table: "oauth_clients",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_device_codes_tenants_tenant_id",
                table: "oauth_device_codes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_grants_tenants_tenant_id",
                table: "oauth_grants",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_patient_devices_tenants_tenant_id",
                table: "patient_devices",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_patient_insulins_tenants_tenant_id",
                table: "patient_insulins",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_patient_records_tenants_tenant_id",
                table: "patient_records",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_profiles_tenants_tenant_id",
                table: "profiles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pump_snapshots_tenants_tenant_id",
                table: "pump_snapshots",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sensitivity_schedules_tenants_tenant_id",
                table: "sensitivity_schedules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sensor_glucose_tenants_tenant_id",
                table: "sensor_glucose",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_settings_tenants_tenant_id",
                table: "settings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_state_spans_tenants_tenant_id",
                table: "state_spans",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_step_counts_tenants_tenant_id",
                table: "step_counts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_system_events_tenants_tenant_id",
                table: "system_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_target_range_schedules_tenants_tenant_id",
                table: "target_range_schedules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_temp_basals_tenants_tenant_id",
                table: "temp_basals",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_therapy_settings_tenants_tenant_id",
                table: "therapy_settings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tracker_definitions_tenants_tenant_id",
                table: "tracker_definitions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tracker_instances_tenants_tenant_id",
                table: "tracker_instances",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tracker_notification_thresholds_tenants_tenant_id",
                table: "tracker_notification_thresholds",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tracker_presets_tenants_tenant_id",
                table: "tracker_presets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatment_foods_tenants_tenant_id",
                table: "treatment_foods",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatments_tenants_tenant_id",
                table: "treatments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_uploader_snapshots_tenants_tenant_id",
                table: "uploader_snapshots",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_food_favorites_tenants_tenant_id",
                table: "user_food_favorites",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activities_tenants_tenant_id",
                table: "activities");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_custom_sounds_tenants_tenant_id",
                table: "alert_custom_sounds");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_deliveries_tenants_tenant_id",
                table: "alert_deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_escalation_steps_tenants_tenant_id",
                table: "alert_escalation_steps");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_excursions_tenants_tenant_id",
                table: "alert_excursions");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_instances_tenants_tenant_id",
                table: "alert_instances");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_invites_tenants_tenant_id",
                table: "alert_invites");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_rules_tenants_tenant_id",
                table: "alert_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_schedules_tenants_tenant_id",
                table: "alert_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_step_channels_tenants_tenant_id",
                table: "alert_step_channels");

            migrationBuilder.DropForeignKey(
                name: "FK_alert_tracker_state_tenants_tenant_id",
                table: "alert_tracker_state");

            migrationBuilder.DropForeignKey(
                name: "FK_aps_snapshots_tenants_tenant_id",
                table: "aps_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_basal_schedules_tenants_tenant_id",
                table: "basal_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_bg_checks_tenants_tenant_id",
                table: "bg_checks");

            migrationBuilder.DropForeignKey(
                name: "FK_body_weights_tenants_tenant_id",
                table: "body_weights");

            migrationBuilder.DropForeignKey(
                name: "FK_bolus_calculations_tenants_tenant_id",
                table: "bolus_calculations");

            migrationBuilder.DropForeignKey(
                name: "FK_boluses_tenants_tenant_id",
                table: "boluses");

            migrationBuilder.DropForeignKey(
                name: "FK_calibrations_tenants_tenant_id",
                table: "calibrations");

            migrationBuilder.DropForeignKey(
                name: "FK_carb_intakes_tenants_tenant_id",
                table: "carb_intakes");

            migrationBuilder.DropForeignKey(
                name: "FK_carb_ratio_schedules_tenants_tenant_id",
                table: "carb_ratio_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_clock_faces_tenants_tenant_id",
                table: "clock_faces");

            migrationBuilder.DropForeignKey(
                name: "FK_compression_low_suggestions_tenants_tenant_id",
                table: "compression_low_suggestions");

            migrationBuilder.DropForeignKey(
                name: "FK_connector_configurations_tenants_tenant_id",
                table: "connector_configurations");

            migrationBuilder.DropForeignKey(
                name: "FK_connector_food_entries_tenants_tenant_id",
                table: "connector_food_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_data_source_metadata_tenants_tenant_id",
                table: "data_source_metadata");

            migrationBuilder.DropForeignKey(
                name: "FK_device_events_tenants_tenant_id",
                table: "device_events");

            migrationBuilder.DropForeignKey(
                name: "FK_devices_tenants_tenant_id",
                table: "devices");

            migrationBuilder.DropForeignKey(
                name: "FK_devicestatus_tenants_tenant_id",
                table: "devicestatus");

            migrationBuilder.DropForeignKey(
                name: "FK_discrepancy_analyses_tenants_tenant_id",
                table: "discrepancy_analyses");

            migrationBuilder.DropForeignKey(
                name: "FK_discrepancy_details_tenants_tenant_id",
                table: "discrepancy_details");

            migrationBuilder.DropForeignKey(
                name: "FK_entries_tenants_tenant_id",
                table: "entries");

            migrationBuilder.DropForeignKey(
                name: "FK_foods_tenants_tenant_id",
                table: "foods");

            migrationBuilder.DropForeignKey(
                name: "FK_heart_rates_tenants_tenant_id",
                table: "heart_rates");

            migrationBuilder.DropForeignKey(
                name: "FK_in_app_notifications_tenants_tenant_id",
                table: "in_app_notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_linked_records_tenants_tenant_id",
                table: "linked_records");

            migrationBuilder.DropForeignKey(
                name: "FK_meter_glucose_tenants_tenant_id",
                table: "meter_glucose");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_tenants_tenant_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_authorization_codes_tenants_tenant_id",
                table: "oauth_authorization_codes");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_clients_tenants_tenant_id",
                table: "oauth_clients");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_device_codes_tenants_tenant_id",
                table: "oauth_device_codes");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_grants_tenants_tenant_id",
                table: "oauth_grants");

            migrationBuilder.DropForeignKey(
                name: "FK_patient_devices_tenants_tenant_id",
                table: "patient_devices");

            migrationBuilder.DropForeignKey(
                name: "FK_patient_insulins_tenants_tenant_id",
                table: "patient_insulins");

            migrationBuilder.DropForeignKey(
                name: "FK_patient_records_tenants_tenant_id",
                table: "patient_records");

            migrationBuilder.DropForeignKey(
                name: "FK_profiles_tenants_tenant_id",
                table: "profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_pump_snapshots_tenants_tenant_id",
                table: "pump_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_sensitivity_schedules_tenants_tenant_id",
                table: "sensitivity_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_sensor_glucose_tenants_tenant_id",
                table: "sensor_glucose");

            migrationBuilder.DropForeignKey(
                name: "FK_settings_tenants_tenant_id",
                table: "settings");

            migrationBuilder.DropForeignKey(
                name: "FK_state_spans_tenants_tenant_id",
                table: "state_spans");

            migrationBuilder.DropForeignKey(
                name: "FK_step_counts_tenants_tenant_id",
                table: "step_counts");

            migrationBuilder.DropForeignKey(
                name: "FK_system_events_tenants_tenant_id",
                table: "system_events");

            migrationBuilder.DropForeignKey(
                name: "FK_target_range_schedules_tenants_tenant_id",
                table: "target_range_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_temp_basals_tenants_tenant_id",
                table: "temp_basals");

            migrationBuilder.DropForeignKey(
                name: "FK_therapy_settings_tenants_tenant_id",
                table: "therapy_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_tracker_definitions_tenants_tenant_id",
                table: "tracker_definitions");

            migrationBuilder.DropForeignKey(
                name: "FK_tracker_instances_tenants_tenant_id",
                table: "tracker_instances");

            migrationBuilder.DropForeignKey(
                name: "FK_tracker_notification_thresholds_tenants_tenant_id",
                table: "tracker_notification_thresholds");

            migrationBuilder.DropForeignKey(
                name: "FK_tracker_presets_tenants_tenant_id",
                table: "tracker_presets");

            migrationBuilder.DropForeignKey(
                name: "FK_treatment_foods_tenants_tenant_id",
                table: "treatment_foods");

            migrationBuilder.DropForeignKey(
                name: "FK_treatments_tenants_tenant_id",
                table: "treatments");

            migrationBuilder.DropForeignKey(
                name: "FK_uploader_snapshots_tenants_tenant_id",
                table: "uploader_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_user_food_favorites_tenants_tenant_id",
                table: "user_food_favorites");

            migrationBuilder.DropIndex(
                name: "IX_uploader_snapshots_tenant_id",
                table: "uploader_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_treatments_tenant_id",
                table: "treatments");

            migrationBuilder.DropIndex(
                name: "IX_treatment_foods_tenant_id",
                table: "treatment_foods");

            migrationBuilder.DropIndex(
                name: "IX_tracker_presets_tenant_id",
                table: "tracker_presets");

            migrationBuilder.DropIndex(
                name: "IX_tracker_notification_thresholds_tenant_id",
                table: "tracker_notification_thresholds");

            migrationBuilder.DropIndex(
                name: "IX_tracker_instances_tenant_id",
                table: "tracker_instances");

            migrationBuilder.DropIndex(
                name: "IX_tracker_definitions_tenant_id",
                table: "tracker_definitions");

            migrationBuilder.DropIndex(
                name: "IX_system_events_tenant_id",
                table: "system_events");

            migrationBuilder.DropIndex(
                name: "IX_step_counts_tenant_id",
                table: "step_counts");

            migrationBuilder.DropIndex(
                name: "IX_state_spans_tenant_id",
                table: "state_spans");

            migrationBuilder.DropIndex(
                name: "IX_pump_snapshots_tenant_id",
                table: "pump_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_profiles_tenant_id",
                table: "profiles");

            migrationBuilder.DropIndex(
                name: "IX_oauth_device_codes_tenant_id",
                table: "oauth_device_codes");

            migrationBuilder.DropIndex(
                name: "IX_oauth_authorization_codes_tenant_id",
                table: "oauth_authorization_codes");

            migrationBuilder.DropIndex(
                name: "IX_meter_glucose_tenant_id",
                table: "meter_glucose");

            migrationBuilder.DropIndex(
                name: "IX_in_app_notifications_tenant_id",
                table: "in_app_notifications");

            migrationBuilder.DropIndex(
                name: "IX_heart_rates_tenant_id",
                table: "heart_rates");

            migrationBuilder.DropIndex(
                name: "IX_entries_tenant_id",
                table: "entries");

            migrationBuilder.DropIndex(
                name: "IX_discrepancy_details_tenant_id",
                table: "discrepancy_details");

            migrationBuilder.DropIndex(
                name: "IX_discrepancy_analyses_tenant_id",
                table: "discrepancy_analyses");

            migrationBuilder.DropIndex(
                name: "IX_devicestatus_tenant_id",
                table: "devicestatus");

            migrationBuilder.DropIndex(
                name: "IX_devices_tenant_id",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "IX_connector_configurations_tenant_id",
                table: "connector_configurations");

            migrationBuilder.DropIndex(
                name: "IX_compression_low_suggestions_tenant_id",
                table: "compression_low_suggestions");

            migrationBuilder.DropIndex(
                name: "IX_clock_faces_tenant_id",
                table: "clock_faces");

            migrationBuilder.DropIndex(
                name: "IX_calibrations_tenant_id",
                table: "calibrations");

            migrationBuilder.DropIndex(
                name: "IX_body_weights_tenant_id",
                table: "body_weights");

            migrationBuilder.DropIndex(
                name: "IX_aps_snapshots_tenant_id",
                table: "aps_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_alert_tracker_state_tenant_id",
                table: "alert_tracker_state");

            migrationBuilder.DropIndex(
                name: "IX_alert_step_channels_tenant_id",
                table: "alert_step_channels");

            migrationBuilder.DropIndex(
                name: "IX_alert_schedules_tenant_id",
                table: "alert_schedules");

            migrationBuilder.DropIndex(
                name: "IX_alert_rules_tenant_id",
                table: "alert_rules");

            migrationBuilder.DropIndex(
                name: "IX_alert_invites_tenant_id",
                table: "alert_invites");

            migrationBuilder.DropIndex(
                name: "IX_alert_instances_tenant_id",
                table: "alert_instances");

            migrationBuilder.DropIndex(
                name: "IX_alert_escalation_steps_tenant_id",
                table: "alert_escalation_steps");

            migrationBuilder.DropIndex(
                name: "IX_alert_deliveries_tenant_id",
                table: "alert_deliveries");

            migrationBuilder.DropIndex(
                name: "IX_alert_custom_sounds_tenant_id",
                table: "alert_custom_sounds");

            migrationBuilder.DropIndex(
                name: "IX_activities_tenant_id",
                table: "activities");
        }
    }
}
