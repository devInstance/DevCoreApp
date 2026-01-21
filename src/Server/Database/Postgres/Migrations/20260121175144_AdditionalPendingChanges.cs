using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("05aa36a6-2b4b-45d6-9ea3-a5cde47dd63c"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("05e33a3f-adb8-45f9-8b31-fd3164e1ec30"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("0c83aa6f-0c68-4f97-8b36-658b7e296bbc"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("0f3ea382-bd33-499b-8647-64087e69289d"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("0f63973b-e6e9-4e1d-8d15-c092aaf254e8"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("0fa73f0e-8cee-4b76-83da-4f47f71dbf93"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("10e2e47d-4e6e-4aee-a9e4-dcd8c6a3cb79"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("12f25e22-b958-42e8-8283-4d07a31f2f9b"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("136287c9-a709-48c7-8a4a-33eab49972d6"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("13baf729-f4b7-4020-8ce7-2fc899ff2eab"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("145c0307-fdf6-4e04-a4e9-76d9fbb73a07"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("150972cb-2f4b-4691-af30-66729a55ecb6"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("15c7028d-e98e-4146-8f5c-93f8d90c1ac2"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("166ea447-8ce2-4139-8ab6-a11d88510e22"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("17d602ab-fc13-445b-9766-3ef4c2297d9b"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("1f4e86ee-ad8d-4b5c-b3f0-77dbbc506428"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("1f8faa9b-c3fe-4201-9e08-43c0aec0bd06"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("21039582-2935-4fd7-a982-365e663a1e18"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("23b7d85b-afaa-423f-893e-0df6224a71a3"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("27d3bbd2-a187-4fbd-8b03-663855865c0a"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("31317c68-f76f-4f66-8382-087c1046d668"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("39ceaf19-d682-4551-895d-33929d558bd7"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("3bf391e7-3036-4f41-a733-be39f3e1e7d5"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("3c6514fa-0087-4888-b7f0-1f920fec087d"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("3d911347-cb95-46b8-ae93-3c27ab77fe9b"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("3ee0d633-09fc-4e0b-ab07-df4620c4fdcb"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("4031e091-cd4d-4a8c-bf60-68db36e46bcf"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("40d04d2e-f429-413e-bfb8-8d16d8dba122"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("42e2c197-4e7f-4f6a-9f5e-a5c0347288b4"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("47f40dc6-c7c9-4901-ab3a-477976ba8e3d"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("489fa051-ac0c-4de2-ba1b-1cbd819834f6"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("4cb15e2a-77d1-46df-86ce-d6e44b9b3f82"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("51704150-1fa6-4b3f-a9e3-233278bae9d4"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("5c19bea1-221e-409a-952b-feaf6b8194ba"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("62826307-2ce7-476a-85e2-6145c917ba99"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("63398d09-1f76-416b-9a3e-c109b99e65ef"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("6583fa67-ac5a-409c-b5a1-1b97969a6170"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("67835124-9bdc-48c8-816a-fcc4ff3e3fed"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("6d6fb3e4-deec-4213-a7f5-65b84cfbffd4"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("6f7f612f-681f-4dc0-af51-0abde4092680"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("6ff4d774-363f-476f-9098-22c78e5d47d2"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("74872830-6b0f-46cf-8782-803dcae58065"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("77bd39c2-a366-4cb6-9b5b-6c7832243118"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("7837c6bf-1492-4f38-af66-83c61c2cb33b"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("7a8c2d0e-02d4-409a-85b8-78d6c38292f0"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("7d862e39-2306-4026-96ec-f67b2a65de25"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("8352ff55-adf0-44e6-9416-1e9cec0f15c6"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("876237fe-fa7a-4e76-a84a-66f223fb14c3"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("8a4ce1bb-37b6-4e4c-9092-13522c75fab6"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("8c9c1a84-e5f3-4985-b28f-5f85c5e2d606"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("90c1c85d-2593-424b-b7a0-1e531f085e0d"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("913e8fad-0fd0-4238-a8cc-46e7d1bff4bd"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("92456678-2200-4797-9bf2-e6939125ebb8"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("930d6bd0-7573-4018-90a2-1f9ebc85b28a"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("938eb043-ed51-40f3-b7a0-f67134f7eda9"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("981a37de-bf0d-4d9b-a1d0-ca54355b82e4"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("99896b58-ab3c-48a7-bcdc-91bda4d7c633"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("9a15b00b-1f9e-4868-bb44-d5505a458cfa"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("9b4c8d55-a05f-4efd-96a2-fdf6c56ac183"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("9b8ce73f-d1d8-4a6c-870b-08777167ac8e"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("9c4c7896-bc0a-4afd-9151-dbf2ad2dfd36"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("9c5cd885-27cd-4f15-ba64-b75945043208"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("9f9481f0-db18-4b97-a81e-6a136a6f06a3"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("a2e94fad-866f-4666-b2f6-5bc08b5dfa0d"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("a319dcea-ef16-4eb5-b3a0-3733fd2d790f"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("a91b260a-0f33-469c-a96c-a8b86b7ba6f9"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("a9d8c8ce-5765-495f-9261-ed88cea4ccdd"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("ab305ca9-5920-4e00-925f-a5b7c3799d81"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("ab936fa6-f02d-4679-9ed1-7a3ef54527ef"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("aec2ef09-78cc-424b-a33f-23d12a42fc4b"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("b011e0c2-263f-4243-89fc-3736831467cf"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("bfaaad6c-03e9-4b5c-bb53-38de3498738f"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("c0187d3c-64ed-4d51-ae7f-c48b74a31614"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("c3e8b316-8336-47a2-ad00-5ef934eae55b"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("c601ab4d-146e-40a4-be3a-755f096f1b8a"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("c6b1d108-2f64-4843-aaaa-eb35cba68b13"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("c88c7d70-a12b-4c65-9ce8-b7bf451e50e7"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("c8bd27b6-d133-44bc-ae6f-d8d3265143e7"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("ca97410c-63b5-46e5-8f97-286cddcb1f43"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("cbe3b43a-bc64-4c2a-9874-ae9e9bbec800"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("ccc46c75-870a-42b1-9f1a-127d4318a585"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("cf62969b-e0a7-4d69-8112-042f16580750"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("d06076db-59fb-47be-bf86-2e7a579b9a4e"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("d30b573a-a00f-44be-9552-d6040cf69fd1"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("d310764b-92bc-457c-84de-a397dda0800b"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("d38093ad-2f7e-4e93-ad3d-ee386dee48b2"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("d6e026ac-c621-417d-bbb0-d6db169e45dc"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("d9cce2b9-c293-4c49-8214-2aedf08b1ecc"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("dbeded2c-1fe1-4867-89e6-67a4a4d183a1"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("dda95f9d-b9eb-422e-b846-75253cde05cb"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("de8e09cd-7594-4e85-a992-308d85c5efb9"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("e12a2c18-16f2-47c3-a6a8-80e7b2cd3fe3"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("e32bf116-d4e3-4a54-a943-d4f75a0e67da"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("eca2e6fe-8a67-4e59-8d9d-3c7655ac2856"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("f38643f9-64cb-4edf-b010-dd612013b8cf"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("f82454e0-520c-46b6-87fe-c7b893173be4"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("fa4b6e93-0dd8-41b6-ab37-9ec7b5943b36"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("fb7ca828-bfd2-4c6b-8c6d-410f07b7acbf"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("fd2ab41e-19fa-4c27-8dd8-efad2d41d555"));

            migrationBuilder.DeleteData(
                table: "WeatherForecasts",
                keyColumn: "Id",
                keyValue: new Guid("ff81987e-86dc-4304-acb5-5951d1ef6227"));

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "UserProfiles",
                newName: "PhoneNumber");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "UserProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "UserProfiles");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "UserProfiles",
                newName: "Name");

            migrationBuilder.InsertData(
                table: "WeatherForecasts",
                columns: new[] { "Id", "CreateDate", "CreatedById", "Date", "PublicId", "Summary", "Temperature", "UpdateDate", "UpdatedById" },
                values: new object[,]
                {
                    { new Guid("05aa36a6-2b4b-45d6-9ea3-a5cde47dd63c"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6559), null, new DateTime(2023, 5, 6, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6557), "8650d8j378q744a48ad8n3fc1084p1n7", "Mild", -1, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6560), null },
                    { new Guid("05e33a3f-adb8-45f9-8b31-fd3164e1ec30"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6444), null, new DateTime(2023, 5, 1, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6442), "pflbi9g5v16472d4h9r7ececa2rf50q9", "Bracing", 24, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6445), null },
                    { new Guid("0c83aa6f-0c68-4f97-8b36-658b7e296bbc"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6194), null, new DateTime(2023, 4, 21, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6192), "f6f4m1hfibt5pff458bei7b446deq9m7", "Scorching", 43, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6196), null },
                    { new Guid("0f3ea382-bd33-499b-8647-64087e69289d"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5974), null, new DateTime(2023, 4, 12, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5972), "c8161aaebaaeb654k9ub86f214l3ibh5", "Cool", -10, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5975), null },
                    { new Guid("0f63973b-e6e9-4e1d-8d15-c092aaf254e8"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7627), null, new DateTime(2023, 6, 22, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7625), "e0dcv1ob4ak1b2b4v942rdf8qf12f0g5", "Sweltering", 14, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7628), null },
                    { new Guid("0fa73f0e-8cee-4b76-83da-4f47f71dbf93"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6430), null, new DateTime(2023, 4, 30, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6428), "s5dc4et158s5s974t9g9o5eetd54q7q1", "Freezing", -17, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6431), null },
                    { new Guid("10e2e47d-4e6e-4aee-a9e4-dcd8c6a3cb79"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6042), null, new DateTime(2023, 4, 15, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6040), "j3jf0a86j7p3udc468fem3obt3s1davf", "Chilly", 4, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6043), null },
                    { new Guid("12f25e22-b958-42e8-8283-4d07a31f2f9b"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7758), null, new DateTime(2023, 6, 28, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7756), "q92ej3l1s9hf2a64kb1864t73cpfc446", "Warm", 14, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7759), null },
                    { new Guid("136287c9-a709-48c7-8a4a-33eab49972d6"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6493), null, new DateTime(2023, 5, 3, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6491), "r516504ej5d6k77418v7e0jfdcr3t7g9", "Chilly", -19, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6494), null },
                    { new Guid("13baf729-f4b7-4020-8ce7-2fc899ff2eab"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7119), null, new DateTime(2023, 5, 31, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7117), "h3kb0cb866rfe4c4mb50l1889ceep7sb", "Bracing", 40, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7121), null },
                    { new Guid("145c0307-fdf6-4e04-a4e9-76d9fbb73a07"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7899), null, new DateTime(2023, 7, 4, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7897), "l1o9acgdl30emb64e808a49ac0mdbea6", "Freezing", -14, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7900), null },
                    { new Guid("150972cb-2f4b-4691-af30-66729a55ecb6"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7612), null, new DateTime(2023, 6, 21, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7610), "62bcg58090hb6cf488hb2aaef298rdt1", "Warm", -6, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7613), null },
                    { new Guid("15c7028d-e98e-4146-8f5c-93f8d90c1ac2"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6251), null, new DateTime(2023, 4, 23, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6249), "jfodg3d6624cn124l91678n7g57cj7r7", "Cool", -12, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6252), null },
                    { new Guid("166ea447-8ce2-4139-8ab6-a11d88510e22"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7743), null, new DateTime(2023, 6, 27, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7741), "a2dam95en9tdq1e44at928odp7hdd4c6", "Sweltering", 45, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7744), null },
                    { new Guid("17d602ab-fc13-445b-9766-3ef4c2297d9b"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7482), null, new DateTime(2023, 6, 15, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7480), "7496b8v17c90g5e418cc04fe0cn9udu1", "Balmy", 2, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7483), null },
                    { new Guid("1f4e86ee-ad8d-4b5c-b3f0-77dbbc506428"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6211), null, new DateTime(2023, 4, 22, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6209), "ceh5nd7eibp3k394i9u3c8i7sbo5q5fe", "Freezing", 27, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6236), null },
                    { new Guid("1f8faa9b-c3fe-4201-9e08-43c0aec0bd06"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6510), null, new DateTime(2023, 5, 4, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6509), "b25a36m5jfsf48a4a86a9en9l5v974q7", "Chilly", 36, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6512), null },
                    { new Guid("21039582-2935-4fd7-a982-365e663a1e18"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7706), null, new DateTime(2023, 6, 26, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7704), "obc0odl1mbb8r314a8c8h1eev5c6o9nf", "Warm", 6, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7707), null },
                    { new Guid("23b7d85b-afaa-423f-893e-0df6224a71a3"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7024), null, new DateTime(2023, 5, 26, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7022), "286e0800n7kb5604q936n3l5j59c62g7", "Mild", 18, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7025), null },
                    { new Guid("27d3bbd2-a187-4fbd-8b03-663855865c0a"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7963), null, new DateTime(2023, 7, 7, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7961), "car5v790o9qbl7f4i9kbf8gdcabcm300", "Balmy", 11, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7964), null },
                    { new Guid("31317c68-f76f-4f66-8382-087c1046d668"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7675), null, new DateTime(2023, 6, 24, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7674), "32m7ifs7gff6888498e0404a2av35ehb", "Warm", -3, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7677), null },
                    { new Guid("39ceaf19-d682-4551-895d-33929d558bd7"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5665), null, new DateTime(2023, 3, 31, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5663), "u77a960474f2cca45846r3hd66of36i7", "Balmy", -12, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5666), null },
                    { new Guid("3bf391e7-3036-4f41-a733-be39f3e1e7d5"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6128), null, new DateTime(2023, 4, 18, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6126), "a0jdp7sdae74l974u9l33eodhbhdlfod", "Cool", 38, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6129), null },
                    { new Guid("3c6514fa-0087-4888-b7f0-1f920fec087d"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6852), null, new DateTime(2023, 5, 19, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6850), "kde8727cn1463eb4pbea1c30nb66ecp7", "Cool", -3, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6853), null },
                    { new Guid("3d911347-cb95-46b8-ae93-3c27ab77fe9b"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6703), null, new DateTime(2023, 5, 13, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6701), "k50e78e478r7p1f4p94c90m7s5c0f480", "Scorching", 13, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6704), null },
                    { new Guid("3ee0d633-09fc-4e0b-ab07-df4620c4fdcb"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6973), null, new DateTime(2023, 5, 24, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6971), "52l18a4e020c1c84c8gbj53aub9akfi9", "Hot", 44, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6974), null },
                    { new Guid("4031e091-cd4d-4a8c-bf60-68db36e46bcf"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7157), null, new DateTime(2023, 6, 1, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7155), "cab69cl9ba0e681478rdk5747cack3l9", "Chilly", 22, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7158), null },
                    { new Guid("40d04d2e-f429-413e-bfb8-8d16d8dba122"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6091), null, new DateTime(2023, 4, 17, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6089), "e06e2ao948f4t944d8u1b6s1kdn3p97c", "Freezing", 8, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6092), null },
                    { new Guid("42e2c197-4e7f-4f6a-9f5e-a5c0347288b4"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6906), null, new DateTime(2023, 5, 21, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6904), "nb2ah3u1188c22a4e8sfjdsdf452f4k5", "Sweltering", 27, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6907), null },
                    { new Guid("47f40dc6-c7c9-4901-ab3a-477976ba8e3d"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7379), null, new DateTime(2023, 6, 11, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7377), "s9q1udc0008e56249826u928gd98d6sb", "Freezing", 1, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7380), null },
                    { new Guid("489fa051-ac0c-4de2-ba1b-1cbd819834f6"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7289), null, new DateTime(2023, 6, 7, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7287), "aaifqfs9vdbe747408a87cg916f482n7", "Sweltering", 52, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7290), null },
                    { new Guid("4cb15e2a-77d1-46df-86ce-d6e44b9b3f82"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6955), null, new DateTime(2023, 5, 23, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6954), "h7m768n7hbob88543a28a8gds756t962", "Freezing", 27, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6957), null },
                    { new Guid("51704150-1fa6-4b3f-a9e3-233278bae9d4"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5943), null, new DateTime(2023, 4, 10, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5941), "aee0u9k38ct196c4o9k5c8ldpb1254ee", "Freezing", 47, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5944), null },
                    { new Guid("5c19bea1-221e-409a-952b-feaf6b8194ba"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7575), null, new DateTime(2023, 6, 20, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7573), "dcdajbl5n9j58414lbkbm388o112s588", "Scorching", 31, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7576), null },
                    { new Guid("62826307-2ce7-476a-85e2-6145c917ba99"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7039), null, new DateTime(2023, 5, 27, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7037), "v346ec68ecsb9814rbhfs5u3l588l95a", "Mild", 25, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7040), null },
                    { new Guid("63398d09-1f76-416b-9a3e-c109b99e65ef"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7321), null, new DateTime(2023, 6, 9, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7319), "g1jf9cufa04496d47838a08c56rf06s7", "Freezing", -14, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7322), null },
                    { new Guid("6583fa67-ac5a-409c-b5a1-1b97969a6170"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7912), null, new DateTime(2023, 7, 5, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7911), "gb68vfeak992r7b4d8c24emdn9ifp3o3", "Warm", 52, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7914), null },
                    { new Guid("67835124-9bdc-48c8-816a-fcc4ff3e3fed"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7947), null, new DateTime(2023, 7, 6, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7945), "o9acqft1b4c8kb04d8pd0cv3ifkdc6de", "Bracing", -20, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7948), null },
                    { new Guid("6d6fb3e4-deec-4213-a7f5-65b84cfbffd4"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7432), null, new DateTime(2023, 6, 13, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7431), "v3sbifa46an548f4i9mblf7e5ad2eehb", "Sweltering", 1, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7433), null },
                    { new Guid("6f7f612f-681f-4dc0-af51-0abde4092680"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6822), null, new DateTime(2023, 5, 17, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6820), "h55esd6c2am5uf84ob82t718e02ci7kd", "Chilly", -1, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6823), null },
                    { new Guid("6ff4d774-363f-476f-9098-22c78e5d47d2"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6920), null, new DateTime(2023, 5, 22, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6918), "jbvfq51atf984ee4lbidu148h9s9v178", "Cool", -12, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6921), null },
                    { new Guid("74872830-6b0f-46cf-8782-803dcae58065"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6392), null, new DateTime(2023, 4, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6390), "d470c8a686j5q3245aubj52chdg1e0h7", "Bracing", 31, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6393), null },
                    { new Guid("77bd39c2-a366-4cb6-9b5b-6c7832243118"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6573), null, new DateTime(2023, 5, 7, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6571), "daq3u1r598g5pdd45a58d49ctfkd40f0", "Freezing", 31, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6574), null },
                    { new Guid("7837c6bf-1492-4f38-af66-83c61c2cb33b"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5959), null, new DateTime(2023, 4, 11, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5957), "gbf4r5d8aap37e648a341ec84e5ce6hb", "Cool", 36, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5960), null },
                    { new Guid("7a8c2d0e-02d4-409a-85b8-78d6c38292f0"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6076), null, new DateTime(2023, 4, 16, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6074), "mdmbq1kf9am5bc94t9m3fatd4cg7qf26", "Bracing", 33, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6077), null },
                    { new Guid("7d862e39-2306-4026-96ec-f67b2a65de25"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5903), null, new DateTime(2023, 4, 9, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5901), "7agff69828982444nbtb56tb52o5tdd2", "Mild", 24, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5904), null },
                    { new Guid("8352ff55-adf0-44e6-9416-1e9cec0f15c6"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6761), null, new DateTime(2023, 5, 15, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6758), "j3e4gbe6pbndj144j97664l7cav706c6", "Scorching", 33, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6762), null },
                    { new Guid("876237fe-fa7a-4e76-a84a-66f223fb14c3"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6327), null, new DateTime(2023, 4, 26, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6325), "0es51e74kdfaj3648ah1def0ce1cgf10", "Cool", 3, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6328), null },
                    { new Guid("8a4ce1bb-37b6-4e4c-9092-13522c75fab6"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7304), null, new DateTime(2023, 6, 8, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7302), "5a16uffav1r9r3e4ibg1ecobib5cn950", "Sweltering", 49, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7305), null },
                    { new Guid("8c9c1a84-e5f3-4985-b28f-5f85c5e2d606"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6778), null, new DateTime(2023, 5, 16, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6776), "g7l7ca6c6a400064nbkf58c8684atdnf", "Freezing", 27, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6779), null },
                    { new Guid("90c1c85d-2593-424b-b7a0-1e531f085e0d"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7884), null, new DateTime(2023, 7, 3, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7882), "583ak9i90eg1bc946888v71cvdq988hb", "Warm", 16, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7885), null },
                    { new Guid("913e8fad-0fd0-4238-a8cc-46e7d1bff4bd"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7415), null, new DateTime(2023, 6, 12, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7413), "ec6072v590b4nff4ub94ib4024v99er3", "Hot", 19, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7416), null },
                    { new Guid("92456678-2200-4797-9bf2-e6939125ebb8"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5873), null, new DateTime(2023, 4, 7, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5871), "qb26m5n36ct10a04rbtbh1b0e6sfl1mb", "Chilly", 7, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5874), null },
                    { new Guid("930d6bd0-7573-4018-90a2-1f9ebc85b28a"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5745), null, new DateTime(2023, 4, 2, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5743), "8a0chf88u7gbk934l90cn92ada7ch7cc", "Bracing", 16, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5746), null },
                    { new Guid("938eb043-ed51-40f3-b7a0-f67134f7eda9"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5763), null, new DateTime(2023, 4, 3, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5761), "58l974tb486a70e4g9ibd4i9gbccp3h7", "Chilly", 34, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5764), null },
                    { new Guid("981a37de-bf0d-4d9b-a1d0-ca54355b82e4"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7447), null, new DateTime(2023, 6, 14, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7445), "nd96b608v356sf74n958ofg11e3avfjd", "Bracing", 4, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7448), null },
                    { new Guid("99896b58-ab3c-48a7-bcdc-91bda4d7c633"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7561), null, new DateTime(2023, 6, 19, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7559), "g3pdvfrf068cb064d8cesbe82e4eb2q3", "Balmy", 25, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7562), null },
                    { new Guid("9a15b00b-1f9e-4868-bb44-d5505a458cfa"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6009), null, new DateTime(2023, 4, 13, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6007), "m5o5aaf0r1n7n504pb62r9p19ebc54f6", "Bracing", 4, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6010), null },
                    { new Guid("9b4c8d55-a05f-4efd-96a2-fdf6c56ac183"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6689), null, new DateTime(2023, 5, 12, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6687), "sdudo5kfpbi79ca46826v32agd76d294", "Mild", -18, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6690), null },
                    { new Guid("9b8ce73f-d1d8-4a6c-870b-08777167ac8e"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6143), null, new DateTime(2023, 4, 19, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6141), "ud5au3l732k7g3345a8c80udeci9m7ec", "Freezing", 35, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6144), null },
                    { new Guid("9c4c7896-bc0a-4afd-9151-dbf2ad2dfd36"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7809), null, new DateTime(2023, 6, 30, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7807), "14l58co552i3a8848at9sbc82086h966", "Hot", 45, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7810), null },
                    { new Guid("9c5cd885-27cd-4f15-ba64-b75945043208"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7823), null, new DateTime(2023, 7, 1, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7821), "u55058t3kf96m7444876mdo7p1m16822", "Mild", 5, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7824), null },
                    { new Guid("9f9481f0-db18-4b97-a81e-6a136a6f06a3"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7222), null, new DateTime(2023, 6, 4, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7220), "4ch768q7642a8e94j9f2d61e4cpdq7ca", "Balmy", 15, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7223), null },
                    { new Guid("a2e94fad-866f-4666-b2f6-5bc08b5dfa0d"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5724), null, new DateTime(2023, 4, 1, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5721), "2ak966kf3ambu3e468v7q12cu3gdf87a", "Scorching", 53, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5725), null },
                    { new Guid("a319dcea-ef16-4eb5-b3a0-3733fd2d790f"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7238), null, new DateTime(2023, 6, 5, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7237), "eec46858h9mde4647ae6n304862ef22c", "Scorching", 6, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7240), null },
                    { new Guid("a91b260a-0f33-469c-a96c-a8b86b7ba6f9"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7641), null, new DateTime(2023, 6, 23, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7639), "k7j1o3v7a4o5r7e4vbl56e96rdp506v1", "Freezing", -4, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7642), null },
                    { new Guid("a9d8c8ce-5765-495f-9261-ed88cea4ccdd"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6892), null, new DateTime(2023, 5, 20, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6889), "88if02o3f4uf0ec4i9e41ef47234m160", "Hot", 2, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6893), null },
                    { new Guid("ab305ca9-5920-4e00-925f-a5b7c3799d81"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6589), null, new DateTime(2023, 5, 8, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6587), "54h1p3c8428cbc042850u5q538d4oft5", "Scorching", 44, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6591), null },
                    { new Guid("ab936fa6-f02d-4679-9ed1-7a3ef54527ef"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7774), null, new DateTime(2023, 6, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7772), "pdo52ckdo3e4m7f4g97afcb2p912t504", "Hot", -16, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7775), null },
                    { new Guid("aec2ef09-78cc-424b-a33f-23d12a42fc4b"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6638), null, new DateTime(2023, 5, 10, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6636), "daj502u1kbib2824m952ifu9f0b0m578", "Cool", -6, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6639), null },
                    { new Guid("b011e0c2-263f-4243-89fc-3736831467cf"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5646), null, new DateTime(2023, 3, 30, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5590), "cc0a0cp7u5289004pb30fek93ad61ce8", "Scorching", 17, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5648), null },
                    { new Guid("bfaaad6c-03e9-4b5c-bb53-38de3498738f"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5888), null, new DateTime(2023, 4, 8, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5886), "2a9ca4u5n948k95408lfdaq9h340dahd", "Balmy", 0, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5889), null },
                    { new Guid("c0187d3c-64ed-4d51-ae7f-c48b74a31614"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6717), null, new DateTime(2023, 5, 14, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6715), "vbqdr3l1aa3ahf746886t3n92a04ofqd", "Mild", 8, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6718), null },
                    { new Guid("c3e8b316-8336-47a2-ad00-5ef934eae55b"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6378), null, new DateTime(2023, 4, 28, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6376), "r3g5u3udhbp5ae34r9a8d8ba1e862cr1", "Scorching", 13, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6379), null },
                    { new Guid("c601ab4d-146e-40a4-be3a-755f096f1b8a"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7252), null, new DateTime(2023, 6, 6, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7250), "m1e4n3s9cc648444obqbc4hf667er5v7", "Balmy", 50, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7253), null },
                    { new Guid("c6b1d108-2f64-4843-aaaa-eb35cba68b13"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7546), null, new DateTime(2023, 6, 18, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7544), "pdm95c3aib66o5b4a8f6h5h5sf98rfp5", "Bracing", -10, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7548), null },
                    { new Guid("c88c7d70-a12b-4c65-9ce8-b7bf451e50e7"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5834), null, new DateTime(2023, 4, 6, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5832), "n5rdg98c8e0aea944822l5k1p9f8b6h7", "Bracing", 18, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5835), null },
                    { new Guid("c8bd27b6-d133-44bc-ae6f-d8d3265143e7"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7365), null, new DateTime(2023, 6, 10, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7363), "h1a0b4r3o9s50434688c74rdn932j9i3", "Freezing", 18, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7366), null },
                    { new Guid("ca97410c-63b5-46e5-8f97-286cddcb1f43"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7105), null, new DateTime(2023, 5, 30, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7103), "k74eqfv7eec4v154p9ibq5q1v9k5d8de", "Warm", -15, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7106), null },
                    { new Guid("cbe3b43a-bc64-4c2a-9874-ae9e9bbec800"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5799), null, new DateTime(2023, 4, 4, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5797), "b4t572nf44g3p5147a28m3f0b616gf56", "Cool", 28, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5800), null },
                    { new Guid("ccc46c75-870a-42b1-9f1a-127d4318a585"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7186), null, new DateTime(2023, 6, 3, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7184), "b8aau344f0287614p914cerft3ld3c3a", "Warm", 32, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7187), null },
                    { new Guid("cf62969b-e0a7-4d69-8112-042f16580750"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7055), null, new DateTime(2023, 5, 28, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7053), "5em5hf64tf96i734u9t9e6r3i7qd9cid", "Chilly", 19, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7056), null },
                    { new Guid("d06076db-59fb-47be-bf86-2e7a579b9a4e"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6653), null, new DateTime(2023, 5, 11, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6651), "k1nd344ak5c490b4g9d67at174f6r15e", "Bracing", 1, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6654), null },
                    { new Guid("d30b573a-a00f-44be-9552-d6040cf69fd1"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7497), null, new DateTime(2023, 6, 16, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7495), "m7n7aeh5jb5and74vbp7kf14k7q9g50c", "Freezing", -13, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7498), null },
                    { new Guid("d310764b-92bc-457c-84de-a397dda0800b"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6158), null, new DateTime(2023, 4, 20, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6156), "o764r3a6e2gf9c548a42bcv764r1s374", "Bracing", 14, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6159), null },
                    { new Guid("d38093ad-2f7e-4e93-ad3d-ee386dee48b2"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6363), null, new DateTime(2023, 4, 27, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6361), "64l7a69a20ifvb24nb3eq9n90858ae40", "Freezing", 30, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6364), null },
                    { new Guid("d6e026ac-c621-417d-bbb0-d6db169e45dc"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6295), null, new DateTime(2023, 4, 24, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6293), "48lff0f6g7i7f6a4v9ibm9i5k5v1e6e0", "Scorching", -8, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6296), null },
                    { new Guid("d9cce2b9-c293-4c49-8214-2aedf08b1ecc"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6837), null, new DateTime(2023, 5, 18, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6835), "g3f2k190u1e6u3e4jbb8acnfs16ae87a", "Hot", 11, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6838), null },
                    { new Guid("dbeded2c-1fe1-4867-89e6-67a4a4d183a1"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6624), null, new DateTime(2023, 5, 9, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6622), "m3e21aaei5kd9a34u9hdf0n7u5hbl914", "Chilly", 44, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6625), null },
                    { new Guid("dda95f9d-b9eb-422e-b846-75253cde05cb"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6989), null, new DateTime(2023, 5, 25, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6987), "3694ufn5i3a8lfe4m9n9h3r5988a0a24", "Freezing", -16, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6990), null },
                    { new Guid("de8e09cd-7594-4e85-a992-308d85c5efb9"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6310), null, new DateTime(2023, 4, 25, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6308), "ccj1749c2elfo5541876s1sbc8r1l3i5", "Hot", 35, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6311), null },
                    { new Guid("e12a2c18-16f2-47c3-a6a8-80e7b2cd3fe3"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7838), null, new DateTime(2023, 7, 2, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7836), "38lfrft1p9104074pb8cs738e2gdlb42", "Warm", 11, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7839), null },
                    { new Guid("e32bf116-d4e3-4a54-a943-d4f75a0e67da"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6027), null, new DateTime(2023, 4, 14, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6025), "30f0f42606kdcc045av7dam536g5i7o1", "Sweltering", 51, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6028), null },
                    { new Guid("eca2e6fe-8a67-4e59-8d9d-3c7655ac2856"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7171), null, new DateTime(2023, 6, 2, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7169), "k5sflbg5c4h9u934ubjbg3ca065c82p3", "Hot", 46, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7173), null },
                    { new Guid("f38643f9-64cb-4edf-b010-dd612013b8cf"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6458), null, new DateTime(2023, 5, 2, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6456), "66e6p1id20lfe674p9tdf086dcf4j7b0", "Freezing", -14, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6459), null },
                    { new Guid("f82454e0-520c-46b6-87fe-c7b893173be4"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7692), null, new DateTime(2023, 6, 25, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7690), "5aj5e672ub54c014aa608el346b2a67a", "Mild", 11, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7693), null },
                    { new Guid("fa4b6e93-0dd8-41b6-ab37-9ec7b5943b36"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6525), null, new DateTime(2023, 5, 5, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6523), "c0o502e6ufa68024lbl5s3dauba6hdnf", "Scorching", 32, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(6526), null },
                    { new Guid("fb7ca828-bfd2-4c6b-8c6d-410f07b7acbf"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5816), null, new DateTime(2023, 4, 5, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5814), "h912hbn3p1l1f03468l5104ct9p9tba6", "Sweltering", 3, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(5817), null },
                    { new Guid("fd2ab41e-19fa-4c27-8dd8-efad2d41d555"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7512), null, new DateTime(2023, 6, 17, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7510), "h794h5d6d2u7n154a8v9lb08l1m3l1p1", "Sweltering", 2, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7513), null },
                    { new Guid("ff81987e-86dc-4304-acb5-5951d1ef6227"), new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7090), null, new DateTime(2023, 5, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7088), "0000h3l180pf8834a86ai348r5u5aco5", "Freezing", 27, new DateTime(2023, 3, 29, 19, 56, 30, 71, DateTimeKind.Local).AddTicks(7092), null }
                });
        }
    }
}
