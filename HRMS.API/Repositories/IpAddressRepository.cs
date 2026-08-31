using DocumentFormat.OpenXml.Bibliography;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Repositories.Interfaces;
using Npgsql;
using System.Data;
using System.Data.Common;

using Npgsql;
using System.Data;

namespace HRMS.API.Repositories
{
    public class IpAddressRepository : BaseRepository, IIpAddressRepository
    {
        private readonly ILogger<IpAddressRepository> _logger;

        public IpAddressRepository(
            IDatabaseConnectionFactory connectionFactory,
            ILogger<IpAddressRepository> logger
        ) : base(connectionFactory)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<IpAddress>> GetAllAsync()
        {
            try
            {
                var sql = @"
                    SELECT id, ipaddressvalue, name, createdat
                    FROM attendance.ipaddresses
                    ORDER BY createdat DESC;
                ";

                return (await QueryAsync<IpAddress>(sql)).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all IP addresses");
                throw;
            }
        }

        public async Task<IpAddress?> GetByIdAsync(Guid id)
        {
            try
            {
                var sql = @"
                    SELECT id, ipaddressvalue, name, createdat
                    FROM attendance.ipaddresses
                    WHERE id = @id;
                ";

                return await QuerySingleOrDefaultAsync<IpAddress>(sql, new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IP address with Id {Id}", id);
                throw;
            }
        }

        public async Task<IpAddress?> GetByIpAsync(string ipAddressValue)
        {
            try
            {
                var sql = @"
                    SELECT id, ipaddressvalue, name, createdat
                    FROM attendance.ipaddresses
                    WHERE ipaddressvalue = @ipAddressValue;
                ";

                return await QuerySingleOrDefaultAsync<IpAddress>(
                    sql,
                    new { ipAddressValue }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IP address {IpAddressValue}", ipAddressValue);
                throw;
            }
        }

        public async Task<bool> CreateAsync(IpAddress ipAddress)
        {
            try
            {
                var sql = @"
                    INSERT INTO attendance.ipaddresses
                    (id, ipaddressvalue, name, createdat)
                    VALUES
                    (@Id, @IpAddressValue, @Name, @CreatedAt);
                ";

                var rows = await ExecuteAsync(sql, ipAddress);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating IP address {IpAddressValue}", ipAddress.IpAddressValue);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(IpAddress ipAddress)
        {
            try
            {
                var sql = @"
                    UPDATE attendance.ipaddresses
                    SET ipaddressvalue = @IpAddressValue,
                        name = @Name
                    WHERE id = @Id;
                ";

                var rows = await ExecuteAsync(sql, ipAddress);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating IP address {Id}", ipAddress.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var sql = @"
                    DELETE FROM attendance.ipaddresses
                    WHERE id = @id;
                ";

                var rows = await ExecuteAsync(sql, new { id });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting IP address {Id}", id);
                throw;
            }
        }
    }
}

