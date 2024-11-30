﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RabbitMqSender.Database;

#nullable disable

namespace RabbitMqSender.Database.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241129172958_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("RabbitMqSender.DataClasses.Entities.Payment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("JsonMessage")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("PaymentStatusId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ReceivedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("PaymentStatusId")
                        .HasDatabaseName("IX_Payment_PaymentStatusId");

                    b.ToTable("Payments");
                });

            modelBuilder.Entity("RabbitMqSender.DataClasses.Entities.PaymentStatus", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("PaymentStatus");

                    b.HasData(
                        new
                        {
                            Id = new Guid("34e89969-aca7-483a-819f-d330292e45ab"),
                            Status = "Получен"
                        },
                        new
                        {
                            Id = new Guid("c4c22208-deb0-4f8b-aa2f-4a4f9fe00a98"),
                            Status = "Передан внешней системе"
                        },
                        new
                        {
                            Id = new Guid("fa294a87-c52a-4fef-b0d3-12ba76e0aa51"),
                            Status = "Ошибка обработки"
                        });
                });

            modelBuilder.Entity("RabbitMqSender.DataClasses.Entities.Payment", b =>
                {
                    b.HasOne("RabbitMqSender.DataClasses.Entities.PaymentStatus", "PaymentStatus")
                        .WithMany()
                        .HasForeignKey("PaymentStatusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PaymentStatus");
                });
#pragma warning restore 612, 618
        }
    }
}
