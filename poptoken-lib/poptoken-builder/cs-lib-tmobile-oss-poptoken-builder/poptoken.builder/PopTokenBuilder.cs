﻿/*
 * Copyright 2020 T-Mobile US, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace com.tmobile.oss.security.taap.poptoken.builder
{
    /// <summary>
    /// A builder class to build the PoP token. PopTokenBuilder is not thread-safe. 
    /// So the instance of this class should not be shared between multiple threads.
    /// </summary>
    public class PopTokenBuilder : IPopTokenBuilder
    {
        private const int POP_TOKEN_VALIDITY_DURATION_IN_MILLIS = 2 * 60 * 1000;
        private const int MAX_NUMBER_OF_EHTS = 100;
        private const string POP_TOKEN_VERSION = "1";
        private readonly string _audience;
        private readonly string _issuer;
        private HashSet<KeyValuePair<string, string>> _ehtsKeyValueMap;
        private string _privateKeyXmlOrPemRsa;
        private RsaSecurityKey _rsaSecurityKey;

        /// <summary>
        /// Default Constructor
        /// </summary>
        private PopTokenBuilder()
        {
        }

        /// <summary>
        /// Custom Constructor
        /// </summary>
        public PopTokenBuilder(string audience, string issuer) : this()
        {
            _audience = audience;
            _issuer = issuer;
        }

        /// <summary>
        /// Returns the Issued At DateTime in UTC.
        /// </summary>
        /// <returns>The issued at time</returns>
        public DateTime IssuedAt
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Returns the version.
        /// </summary>
        /// <returns>The version</returns>
        public string Version
        {
            get
            {
                return POP_TOKEN_VERSION;
            }
        }

        /// <summary>
        /// Sets the ehtsKeyValueMap, which is a HashSet of KeyValuePair. 
        /// It is used to calculate the values of ehts (external headers to sign) and edts (hash of external data to sign).
        /// </summary>
        /// <param name="ehtsKeyValueMap">HashSet of headers and data</param>
        /// <returns>The PopTokenBuilder</returns>
        public PopTokenBuilder SetEhtsKeyValueMap(HashSet<KeyValuePair<string, string>> ehtsKeyValueMap)
        {
            _ehtsKeyValueMap = ehtsKeyValueMap;
            return this;
        }

        /// <summary>
        /// Signs the PoP token using RSA private key in XML or PEM format
        /// </summary>
        /// <param name="privateKeyXmlRsa">The RSA private key in XML or PEM format</param>
        /// <returns>The PopTokenBuilder</returns>
        public PopTokenBuilder SignWith(string privateKeyXmlOrPemRsa)
        {
            if (string.IsNullOrEmpty(privateKeyXmlOrPemRsa))
            {
                throw new PopTokenBuilderException("A privateKeyXmlOrPemRsa shall be provided to sign the PoP token");
            }

            _privateKeyXmlOrPemRsa = privateKeyXmlOrPemRsa;
            return this;
        }

        /// <summary>
        /// Returns the expiration time.
        /// </summary>
        /// <param name="issuedAt"> The time when the PoP token is issued </param>
        /// <returns> The expiration time </returns>
        public DateTime GetExpiration(DateTime issuedAt)
        {
            return issuedAt.AddMilliseconds((double)POP_TOKEN_VALIDITY_DURATION_IN_MILLIS);
        }

        /// <summary>
        /// Returns the unique identifier.
        /// </summary>
        /// <returns> The unique identifier </returns>
        public string GetUniqueIdentifier()
        {
            return Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Builds, signs and returns the string representation of the PoP token. The PoP token will be valid for 2 minutes.
        /// </summary>
        /// <returns> The PoP token </returns>
        /// <exception cref="PopTokenBuilderException"> if error occurs while building the PoP token </exception>
        public string Build()
        {
            try
            {
                if (_ehtsKeyValueMap == null ||
                    _ehtsKeyValueMap.Count == 0 ||
                    DoesContainAnyEmptyKeysOrValues(_ehtsKeyValueMap))
                {
                    throw new PopTokenBuilderException("The ehtsKeyValueMap should not be null or empty and should not contain any null or empty ehts keys or values");
                }

                if (_ehtsKeyValueMap.Count > MAX_NUMBER_OF_EHTS)
                {
                    throw new PopTokenBuilderException("The ehtsKeyValueMap should not contain more than " + MAX_NUMBER_OF_EHTS + " entries");
                }

                if (string.IsNullOrEmpty(_privateKeyXmlOrPemRsa))
                {
                    throw new PopTokenBuilderException("The privateKeyXmlRsa should be provided to SignWith the PoP token");
                }

                _rsaSecurityKey = PopTokenBuilderUtils.CreateRsaSecurityKey(_privateKeyXmlOrPemRsa);

                var ehts = BuildEhtsString(_ehtsKeyValueMap);
                var edts = CalculateEdtsSha256Base64Hash(_ehtsKeyValueMap);
                var jti = GetUniqueIdentifier();
                var version = Version;
                var issuedAt = IssuedAt;
                var expires = GetExpiration(issuedAt);
                var signingCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256);
                var securityTokenDescriptor = new SecurityTokenDescriptor()
                {
                    Subject = new ClaimsIdentity(new List<Claim>
                {
                    new Claim("ehts", ehts),
                    new Claim("edts", edts),
                    new Claim("v", version),
                    new Claim("jti", jti)
                }),
                    Audience = _audience,
                    Issuer = _issuer,
                    NotBefore = issuedAt,
                    IssuedAt = issuedAt,
                    Expires = expires,
                    SigningCredentials = signingCredentials
                };

                var jsonWebTokenHandler = new JsonWebTokenHandler();
                return jsonWebTokenHandler.CreateToken(securityTokenDescriptor); // Any instance members are not guaranteed to be thread safe.
            }
            catch (PopTokenBuilderException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new PopTokenBuilderException("Error occurred while building the PoP token builder, error: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Returns true if the ehtsKeyValueMap contains any empty keys
        /// </summary>
        /// <param name="ehtsKeyValueMap">
        /// @return </param>
        private bool DoesContainAnyEmptyKeysOrValues(HashSet<KeyValuePair<string, string>> ehtsKeyValueMap)
        {
            foreach (var ehtsKeyValueEntry in ehtsKeyValueMap)
            {
                if (string.IsNullOrEmpty(ehtsKeyValueEntry.Key) ||
                    string.IsNullOrEmpty(ehtsKeyValueEntry.Value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds the semicolon separated ehts string using the specified ehtsKeysList
        /// </summary>
        /// <param name="ehtsKeyValueMap"> The ehts keys list </param>
        /// <returns> the ehts string containing the semicolon delimited ehts keys </returns>
        private string BuildEhtsString(HashSet<KeyValuePair<string, string>> ehtsKeyValueMap)
        {
            var ehtsStringBuilder = new StringBuilder();

            foreach (var ehtsKey in ehtsKeyValueMap)
            {
                if (ehtsStringBuilder.Length > 0)
                {
                    ehtsStringBuilder.Append(";");
                }

                ehtsStringBuilder.Append(ehtsKey.Key.Replace("\n", "")
                                                    .Replace("\r", ""));
            }

            return ehtsStringBuilder.ToString();
        }

        /// <summary>
        /// Calculates the edts (hash of external data to sign) in the same sequence as provided in the LinkedHashMap.
        /// </summary>
        /// <param name="ehtsKeysList"> </param>
        /// <param name="ehtsKeyValueMap"> </param>
        /// <returns> The edts (hash of external data to sign) </returns>
        private string CalculateEdtsSha256Base64Hash(HashSet<KeyValuePair<string, string>> ehtsKeyValueMap)
        {
            var stringToHashBuilder = new StringBuilder();

            foreach (var ehtsKeyValueEntry in ehtsKeyValueMap)
            {
                stringToHashBuilder.Append(ehtsKeyValueEntry.Value
                                                            .Replace("\n", "")
                                                            .Replace("\r", ""));
            }

            var stringToHash = stringToHashBuilder.ToString();

            return Sha256Base64UrlSafeString(stringToHash);
        }

        /// <summary>
        /// Creates SHA 256 base64 URL safe encrypted hash of the specified string.
        /// </summary>
        /// <param name="stringToHash"> The string to be hashed </param>
        /// <returns> SHA 256 base64 encrypted hash. </returns>
        private string Sha256Base64UrlSafeString(string stringToHash)
        {
            var inputBytes = Encoding.ASCII.GetBytes(stringToHash);
            var sha256 = new SHA256Managed();
            var hash = sha256.ComputeHash(inputBytes);

            return Convert.ToBase64String(hash).Replace("=", "")
                                               .Replace("+", "-")
                                               .Replace("/", "_");
        }
    }
}
