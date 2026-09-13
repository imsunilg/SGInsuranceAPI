using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGInsurance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SGInsurance");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "SGInsurance",
                columns: table => new
                {
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.audit_id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "SGInsurance",
                columns: table => new
                {
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pincode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.customer_id);
                });

            migrationBuilder.CreateTable(
                name: "fuel_types",
                schema: "SGInsurance",
                columns: table => new
                {
                    fuel_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fuel_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_types", x => x.fuel_code);
                });

            migrationBuilder.CreateTable(
                name: "kyc_verifications",
                schema: "SGInsurance",
                columns: table => new
                {
                    kyc_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kyc_verifications", x => x.kyc_id);
                });

            migrationBuilder.CreateTable(
                name: "lob_master",
                schema: "SGInsurance",
                columns: table => new
                {
                    lob_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lob_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lob_master", x => x.lob_code);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                schema: "SGInsurance",
                columns: table => new
                {
                    template_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    body_template = table.Column<string>(type: "text", nullable: false),
                    placeholders = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_templates", x => x.template_code);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "SGInsurance",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    recipient = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.notification_id);
                });

            migrationBuilder.CreateTable(
                name: "payment_modes",
                schema: "SGInsurance",
                columns: table => new
                {
                    mode_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    mode_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_modes", x => x.mode_code);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "SGInsurance",
                columns: table => new
                {
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.payment_id);
                });

            migrationBuilder.CreateTable(
                name: "policies",
                schema: "SGInsurance",
                columns: table => new
                {
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lob_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    coverage_data = table.Column<string>(type: "jsonb", nullable: false),
                    sum_insured = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    total_premium = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    gst_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    risk_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    risk_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policies", x => x.policy_id);
                });

            migrationBuilder.CreateTable(
                name: "policy_documents",
                schema: "SGInsurance",
                columns: table => new
                {
                    doc_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_documents", x => x.doc_id);
                });

            migrationBuilder.CreateTable(
                name: "proposals",
                schema: "SGInsurance",
                columns: table => new
                {
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    proposal_data = table.Column<string>(type: "jsonb", nullable: false),
                    nominee_data = table.Column<string>(type: "jsonb", nullable: false),
                    payment_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposals", x => x.proposal_id);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                schema: "SGInsurance",
                columns: table => new
                {
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lob_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_data = table.Column<string>(type: "jsonb", nullable: false),
                    sum_insured = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    base_premium = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    addon_premium = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ncb_discount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    gst_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    total_premium = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.quote_id);
                });

            migrationBuilder.CreateTable(
                name: "risk_verifications",
                schema: "SGInsurance",
                columns: table => new
                {
                    verification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    verification_data = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_verifications", x => x.verification_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "SGInsurance",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "SGInsurance",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_makes",
                schema: "SGInsurance",
                columns: table => new
                {
                    make_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    make_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_makes", x => x.make_code);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_models",
                schema: "SGInsurance",
                columns: table => new
                {
                    model_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    make_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_models", x => x.model_code);
                });

            migrationBuilder.CreateTable(
                name: "product_master",
                schema: "SGInsurance",
                columns: table => new
                {
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lob_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: false),
                    rating_strategy_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_master", x => x.product_code);
                    table.ForeignKey(
                        name: "FK_product_master_lob_master_lob_code",
                        column: x => x.lob_code,
                        principalSchema: "SGInsurance",
                        principalTable: "lob_master",
                        principalColumn: "lob_code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                schema: "SGInsurance",
                columns: table => new
                {
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_attempts", x => x.attempt_id);
                    table.ForeignKey(
                        name: "FK_payment_attempts_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "SGInsurance",
                        principalTable: "payments",
                        principalColumn: "payment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quote_addons",
                schema: "SGInsurance",
                columns: table => new
                {
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    addon_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_addons", x => new { x.quote_id, x.addon_code });
                    table.ForeignKey(
                        name: "FK_quote_addons_quotes_quote_id",
                        column: x => x.quote_id,
                        principalSchema: "SGInsurance",
                        principalTable: "quotes",
                        principalColumn: "quote_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "SGInsurance",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "SGInsurance",
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "SGInsurance",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "premium_rules",
                schema: "SGInsurance",
                columns: table => new
                {
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rule_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premium_rules", x => x.rule_id);
                    table.ForeignKey(
                        name: "FK_premium_rules_product_master_product_code",
                        column: x => x.product_code,
                        principalSchema: "SGInsurance",
                        principalTable: "product_master",
                        principalColumn: "product_code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_addons",
                schema: "SGInsurance",
                columns: table => new
                {
                    addon_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    addon_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    base_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_addons", x => new { x.addon_code, x.product_code });
                    table.ForeignKey(
                        name: "FK_product_addons_product_master_product_code",
                        column: x => x.product_code,
                        principalSchema: "SGInsurance",
                        principalTable: "product_master",
                        principalColumn: "product_code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_email",
                schema: "SGInsurance",
                table: "customers",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_mobile",
                schema: "SGInsurance",
                table: "customers",
                column: "mobile",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_payment_id",
                schema: "SGInsurance",
                table: "payment_attempts",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_policies_policy_number",
                schema: "SGInsurance",
                table: "policies",
                column: "policy_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_premium_rules_product_code",
                schema: "SGInsurance",
                table: "premium_rules",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "IX_product_addons_product_code",
                schema: "SGInsurance",
                table: "product_addons",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "IX_product_master_lob_code",
                schema: "SGInsurance",
                table: "product_master",
                column: "lob_code");

            migrationBuilder.CreateIndex(
                name: "IX_roles_role_code",
                schema: "SGInsurance",
                table: "roles",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                schema: "SGInsurance",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "SGInsurance",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "fuel_types",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "kyc_verifications",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "notification_templates",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "payment_attempts",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "payment_modes",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "policies",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "policy_documents",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "premium_rules",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "product_addons",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "proposals",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "quote_addons",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "risk_verifications",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "vehicle_makes",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "vehicle_models",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "product_master",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "quotes",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "users",
                schema: "SGInsurance");

            migrationBuilder.DropTable(
                name: "lob_master",
                schema: "SGInsurance");
        }
    }
}
