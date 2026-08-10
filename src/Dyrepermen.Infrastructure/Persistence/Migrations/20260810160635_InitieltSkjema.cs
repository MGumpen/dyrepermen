using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitieltSkjema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asp_net_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "husstand",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    navn = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    opprettet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_husstand", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    husstand_id = table.Column<int>(type: "integer", nullable: true),
                    visningsnavn = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_users_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dyr",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    husstand_id = table.Column<int>(type: "integer", nullable: false),
                    navn = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    art = table.Column<char>(type: "char(1)", nullable: false),
                    rase = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    kjonn = table.Column<char>(type: "char(1)", nullable: false),
                    fodselsdato = table.Column<DateOnly>(type: "date", nullable: true),
                    chip_nr = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    reg_nr_nkk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    kastrert = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    bilde_filnavn = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    foringslogg_aktiv = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    forplan_aktiv = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    aktiv = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dyr", x => x.id);
                    table.CheckConstraint("ck_dyr_art", "art IN ('H','K')");
                    table.CheckConstraint("ck_dyr_chip_lengde", "chip_nr IS NULL OR char_length(chip_nr) = 15");
                    table.CheckConstraint("ck_dyr_kjonn", "kjonn IN ('T','H')");
                    table.ForeignKey(
                        name: "fk_dyr_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "husstand_innstilling",
                columns: table => new
                {
                    husstand_id = table.Column<int>(type: "integer", nullable: false),
                    foringslogg_standard = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    forplan_standard = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    varsler_aktiv = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_husstand_innstilling", x => x.husstand_id);
                    table.ForeignKey(
                        name: "fk_husstand_innstilling_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_roles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_tokens",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "husstand_invitasjon",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    husstand_id = table.Column<int>(type: "integer", nullable: false),
                    epost = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    innlost_av_bruker_id = table.Column<int>(type: "integer", nullable: true),
                    innlost_tid = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    opprettet_av_bruker_id = table.Column<int>(type: "integer", nullable: true),
                    opprettet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_husstand_invitasjon", x => x.id);
                    table.ForeignKey(
                        name: "fk_husstand_invitasjon_asp_net_users_innlost_av_bruker_id",
                        column: x => x.innlost_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_husstand_invitasjon_asp_net_users_opprettet_av_bruker_id",
                        column: x => x.opprettet_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_husstand_invitasjon_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "behandling",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<char>(type: "char(1)", nullable: false),
                    preparat = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    dato = table.Column<DateOnly>(type: "date", nullable: false),
                    neste_dato = table.Column<DateOnly>(type: "date", nullable: true),
                    notat = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_behandling", x => x.id);
                    table.CheckConstraint("ck_behandling_type", "type IN ('V','O','F','K','T')");
                    table.ForeignKey(
                        name: "fk_behandling_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dokument",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    filnavn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    originalnavn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kategori = table.Column<char>(type: "char(1)", nullable: false),
                    opplastet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dokument", x => x.id);
                    table.CheckConstraint("ck_dokument_kategori", "kategori IN ('V','J','K','A')");
                    table.ForeignKey(
                        name: "fk_dokument_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "foring",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    tidspunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    mengde_gram = table.Column<int>(type: "integer", nullable: true),
                    gitt_av_bruker_id = table.Column<int>(type: "integer", nullable: true),
                    kommentar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_foring", x => x.id);
                    table.CheckConstraint("ck_foring_mengde", "mengde_gram IS NULL OR mengde_gram > 0");
                    table.ForeignKey(
                        name: "fk_foring_asp_net_users_gitt_av_bruker_id",
                        column: x => x.gitt_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_foring_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forplan",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    metode = table.Column<char>(type: "char(1)", nullable: false),
                    prosent_tidels = table.Column<int>(type: "integer", nullable: true),
                    gram_per_dag = table.Column<int>(type: "integer", nullable: true),
                    antall_maltider = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    fornavn = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    notat = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    aktiv = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    opprettet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forplan", x => x.id);
                    table.CheckConstraint("ck_forplan_maltider", "antall_maltider BETWEEN 1 AND 6");
                    table.CheckConstraint("ck_forplan_metode", "metode IN ('P','G')");
                    table.CheckConstraint("ck_forplan_verdi", "   (metode = 'P' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL)\nOR (metode = 'G' AND gram_per_dag IS NOT NULL\n                 AND gram_per_dag > 0\n                 AND prosent_tidels IS NULL)");
                    table.ForeignKey(
                        name: "fk_forplan_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forsikring",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    selskap = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    polise_nr = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    arspremie_kr = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    egenandel = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    fornyes_dato = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forsikring", x => x.id);
                    table.ForeignKey(
                        name: "fk_forsikring_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "handleliste",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    husstand_id = table.Column<int>(type: "integer", nullable: false),
                    dyr_id = table.Column<int>(type: "integer", nullable: true),
                    tekst = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    antall = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    status = table.Column<char>(type: "char(1)", nullable: false, defaultValue: 'A'),
                    opprettet_av_bruker_id = table.Column<int>(type: "integer", nullable: true),
                    opprettet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_handleliste", x => x.id);
                    table.CheckConstraint("ck_handleliste_status", "status IN ('A','K')");
                    table.ForeignKey(
                        name: "fk_handleliste_asp_net_users_opprettet_av_bruker_id",
                        column: x => x.opprettet_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_handleliste_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_handleliste_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medisin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    navn = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    dose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    intervall_timer = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    start_dato = table.Column<DateOnly>(type: "date", nullable: false),
                    slutt_dato = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medisin", x => x.id);
                    table.ForeignKey(
                        name: "fk_medisin_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vekt",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    vekt_gram = table.Column<int>(type: "integer", nullable: false),
                    dato = table.Column<DateOnly>(type: "date", nullable: false),
                    registrert_av_bruker_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vekt", x => x.id);
                    table.CheckConstraint("ck_vekt_gram", "vekt_gram > 0");
                    table.ForeignKey(
                        name: "fk_vekt_asp_net_users_registrert_av_bruker_id",
                        column: x => x.registrert_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_vekt_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vetbesok",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    dyr_id = table.Column<int>(type: "integer", nullable: false),
                    dato = table.Column<DateOnly>(type: "date", nullable: false),
                    klinikk = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    arsak = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    diagnose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    kostnad_kr = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    forsikring_krevd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vetbesok", x => x.id);
                    table.ForeignKey(
                        name: "fk_vetbesok_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dose",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    medisin_id = table.Column<int>(type: "integer", nullable: false),
                    gitt_tid = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    gitt_av_bruker_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dose", x => x.id);
                    table.ForeignKey(
                        name: "fk_dose_asp_net_users_gitt_av_bruker_id",
                        column: x => x.gitt_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_dose_medisin_medisin_id",
                        column: x => x.medisin_id,
                        principalTable: "medisin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "asp_net_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ux_asp_net_roles_navn",
                table: "asp_net_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "asp_net_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "asp_net_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "asp_net_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_epost",
                table: "asp_net_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_bruker_husstand",
                table: "asp_net_users",
                column: "husstand_id");

            migrationBuilder.CreateIndex(
                name: "ux_asp_net_users_brukernavn",
                table: "asp_net_users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_behandling_dyr_id",
                table: "behandling",
                column: "dyr_id");

            migrationBuilder.CreateIndex(
                name: "ix_behandling_neste",
                table: "behandling",
                column: "neste_dato",
                filter: "neste_dato IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_dokument_dyr_id",
                table: "dokument",
                column: "dyr_id");

            migrationBuilder.CreateIndex(
                name: "ix_dose_gitt_av_bruker_id",
                table: "dose",
                column: "gitt_av_bruker_id");

            migrationBuilder.CreateIndex(
                name: "ix_dose_medisin_tid",
                table: "dose",
                columns: new[] { "medisin_id", "gitt_tid" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_dyr_husstand",
                table: "dyr",
                column: "husstand_id");

            migrationBuilder.CreateIndex(
                name: "ux_dyr_chip",
                table: "dyr",
                column: "chip_nr",
                unique: true,
                filter: "chip_nr IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_foring_dyr_tid",
                table: "foring",
                columns: new[] { "dyr_id", "tidspunkt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_foring_gitt_av_bruker_id",
                table: "foring",
                column: "gitt_av_bruker_id");

            migrationBuilder.CreateIndex(
                name: "ux_forplan_aktiv",
                table: "forplan",
                column: "dyr_id",
                unique: true,
                filter: "aktiv");

            migrationBuilder.CreateIndex(
                name: "ix_forsikring_dyr_id",
                table: "forsikring",
                column: "dyr_id");

            migrationBuilder.CreateIndex(
                name: "ix_handleliste_aktiv",
                table: "handleliste",
                column: "husstand_id",
                filter: "status = 'A'");

            migrationBuilder.CreateIndex(
                name: "ix_handleliste_dyr_id",
                table: "handleliste",
                column: "dyr_id");

            migrationBuilder.CreateIndex(
                name: "ix_handleliste_opprettet_av_bruker_id",
                table: "handleliste",
                column: "opprettet_av_bruker_id");

            migrationBuilder.CreateIndex(
                name: "ix_husstand_invitasjon_husstand_id",
                table: "husstand_invitasjon",
                column: "husstand_id");

            migrationBuilder.CreateIndex(
                name: "ix_husstand_invitasjon_innlost_av_bruker_id",
                table: "husstand_invitasjon",
                column: "innlost_av_bruker_id");

            migrationBuilder.CreateIndex(
                name: "ix_husstand_invitasjon_opprettet_av_bruker_id",
                table: "husstand_invitasjon",
                column: "opprettet_av_bruker_id");

            migrationBuilder.CreateIndex(
                name: "ix_medisin_dyr_id",
                table: "medisin",
                column: "dyr_id");

            migrationBuilder.CreateIndex(
                name: "ix_vekt_dyr_dato",
                table: "vekt",
                columns: new[] { "dyr_id", "dato" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_vekt_registrert_av_bruker_id",
                table: "vekt",
                column: "registrert_av_bruker_id");

            migrationBuilder.CreateIndex(
                name: "ix_vetbesok_dyr_id",
                table: "vetbesok",
                column: "dyr_id");

            // ----------------------------------------------------------------
            // Funksjonelle indekser. EF Core kan ikke uttrykke en indeks over
            // et uttrykk - kun over kolonner - sa disse ma skrives som ra SQL.
            //
            // Konsekvens: modell-snapshotet kjenner dem ikke. De blir staende
            // i databasen og maa vedlikeholdes her. Endres kolonnene de bygger
            // pa, ma indeksene endres i en ny migrasjon manuelt.
            // ----------------------------------------------------------------

            // Regnummer sammenlignes uten hensyn til store bokstaver.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ux_dyr_regnr
                    ON dyr (upper(reg_nr_nkk))
                    WHERE reg_nr_nkk IS NOT NULL;
                """);

            // En ventende invitasjon per adresse pa tvers av hele systemet.
            // Kun ventende - innloste rader skal kunne ligge flere av.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ux_invitasjon_epost
                    ON husstand_invitasjon (lower(epost))
                    WHERE innlost_tid IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_invitasjon_epost;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_dyr_regnr;");

            migrationBuilder.DropTable(
                name: "asp_net_role_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_logins");

            migrationBuilder.DropTable(
                name: "asp_net_user_roles");

            migrationBuilder.DropTable(
                name: "asp_net_user_tokens");

            migrationBuilder.DropTable(
                name: "behandling");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "dokument");

            migrationBuilder.DropTable(
                name: "dose");

            migrationBuilder.DropTable(
                name: "foring");

            migrationBuilder.DropTable(
                name: "forplan");

            migrationBuilder.DropTable(
                name: "forsikring");

            migrationBuilder.DropTable(
                name: "handleliste");

            migrationBuilder.DropTable(
                name: "husstand_innstilling");

            migrationBuilder.DropTable(
                name: "husstand_invitasjon");

            migrationBuilder.DropTable(
                name: "vekt");

            migrationBuilder.DropTable(
                name: "vetbesok");

            migrationBuilder.DropTable(
                name: "asp_net_roles");

            migrationBuilder.DropTable(
                name: "medisin");

            migrationBuilder.DropTable(
                name: "asp_net_users");

            migrationBuilder.DropTable(
                name: "dyr");

            migrationBuilder.DropTable(
                name: "husstand");
        }
    }
}
