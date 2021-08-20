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

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace com.tmobile.oss.security.taap.jwe.test
{
	[TestClass]
	public class EncryptionTest
	{
		private Mock<IKeyResolver> publicRSAKeyResolver;
		private Mock<IKeyResolver> privateRSAKeyResolver;

		private Mock<IKeyResolver> publicECKeyResolver;
		private Mock<IKeyResolver> privateECKeyResolver;

		private Mock<IKeyResolver> publicOctKeyResolver;
		private Mock<IKeyResolver> privateOctKeyResolver;

		private Mock<IKeyResolver> publicNullKeyResolver;

		private Mock<ILogger<Encryption>> logger;

		[TestInitialize()]
		public void TestInitialize()
		{
			// RSA Public Key
			var jwksJson = File.ReadAllText(@"TestData\JwksRSAPublic.json");
			var jwks = JsonConvert.DeserializeObject<Jwks>(jwksJson);
			var rsaPublicJsonWebKey = jwks.Keys[0];

			this.publicRSAKeyResolver = new Mock<IKeyResolver>();
			this.publicRSAKeyResolver.Setup(k => k.GetEncryptionKeyAsync())
									 .ReturnsAsync(() => rsaPublicJsonWebKey);

			// RSA Private Key
			var jsonWebKey = File.ReadAllText(@"TestData\RSAPrivate.json");
			var rsaPrivateJsonWebKey = JsonConvert.DeserializeObject<JsonWebKey>(jsonWebKey);
			this.privateRSAKeyResolver = new Mock<IKeyResolver>();
			this.privateRSAKeyResolver.Setup(k => k.GetDecryptionKeyAsync(rsaPrivateJsonWebKey.Kid))
									  .ReturnsAsync(() => rsaPrivateJsonWebKey);

			// EC Public Key
			jwksJson = File.ReadAllText(@"TestData\JwksECPublic.json");
			jwks = JsonConvert.DeserializeObject<Jwks>(jwksJson);
			var ecPublicJsonWebKey = jwks.Keys[0];
			this.publicECKeyResolver = new Mock<IKeyResolver>();
			this.publicECKeyResolver.Setup(k => k.GetEncryptionKeyAsync())
									.ReturnsAsync(() => ecPublicJsonWebKey);

			// EC Private Key
			jsonWebKey = File.ReadAllText(@"TestData\ECPrivate.json");
			var ecPrivateJsonWebKey = JsonConvert.DeserializeObject<JsonWebKey>(jsonWebKey);
			this.privateECKeyResolver = new Mock<IKeyResolver>();
			this.privateECKeyResolver.Setup(k => k.GetDecryptionKeyAsync(ecPrivateJsonWebKey.Kid))
									 .ReturnsAsync(() => ecPrivateJsonWebKey);

			// Oct Public Key
			jwksJson = File.ReadAllText(@"TestData\JwksOctPublic.json");
			jwks = JsonConvert.DeserializeObject<Jwks>(jwksJson);
			var octPublicJsonWebKey = jwks.Keys[0];
			this.publicOctKeyResolver = new Mock<IKeyResolver>();
			this.publicOctKeyResolver.Setup(k => k.GetEncryptionKeyAsync())
									 .ReturnsAsync(() => octPublicJsonWebKey);

			// Oct Private Key
			jsonWebKey = File.ReadAllText(@"TestData\OctPrivate.json");
			var octPrivateJsonWebKey = JsonConvert.DeserializeObject<JsonWebKey>(jsonWebKey);
			this.privateOctKeyResolver = new Mock<IKeyResolver>();
			this.privateOctKeyResolver.Setup(k => k.GetDecryptionKeyAsync(octPrivateJsonWebKey.Kid))
									  .ReturnsAsync(() => octPrivateJsonWebKey);

			// Null Public Keys
			this.publicNullKeyResolver = new Mock<IKeyResolver>();
			this.publicNullKeyResolver.Setup(k => k.GetEncryptionKeyAsync())
									 .ReturnsAsync(() => null);

			this.logger = new Mock<ILogger<Encryption>>();
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		public async Task EncryptAsync_RSA_Success()
		{
			// Arrange
			string expectedValue = "Test data!";
			var publicEncryption = new Encryption(this.publicRSAKeyResolver.Object, this.logger.Object);
			var privateEncryption = new Encryption(this.privateRSAKeyResolver.Object, this.logger.Object);

			// Act
			string cipherData = await publicEncryption.EncryptAsync(expectedValue);
			string actualValue = await privateEncryption.DecryptAsync(cipherData);

			// Assert
			Assert.AreEqual(expectedValue, actualValue);
		}


		[TestMethod]
		[TestCategory("UnitTest")]
		public async Task DecryptAsync_RSA_Success()
		{
			// Arrange
			string expectedValue = "Test data!";
			var publicEncryption = new Encryption(this.publicRSAKeyResolver.Object, this.logger.Object);
			var privateEncryption = new Encryption(this.privateRSAKeyResolver.Object, this.logger.Object);

			// Act
			string cipherData = await publicEncryption.EncryptAsync(expectedValue);
			string actualValue = await privateEncryption.DecryptAsync(cipherData);

			// Assert
			Assert.AreEqual(expectedValue, actualValue);
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		public async Task EncryptAsync_EC_Success()
		{
			// Arrange
			string expectedValue = "Test data!";
			var publicEncryption = new Encryption(this.publicECKeyResolver.Object, this.logger.Object);
			var privateEncryption = new Encryption(this.privateECKeyResolver.Object, this.logger.Object);

			// Act
			string cipherData = await publicEncryption.EncryptAsync(expectedValue);
			string actualValue = await privateEncryption.DecryptAsync(cipherData);


			// Assert
			Assert.AreEqual(expectedValue, actualValue);
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		public async Task DecryptAsync_EC_Success()
		{
			// Arrange
			string expectedValue = "Test data!";
			var cipherData = "{cipher}eyJhbGciOiJFQ0RILUVTK0EyNTZLVyIsImVuYyI6IkEyNTZHQ00iLCJraWQiOiJCN0I0RjVDNy0yQjQ2LTRGNTQtQTgxQS01MUU4QTg4NkIwOTQiLCJrdHkiOiJFQyIsImVwayI6eyJrdHkiOiJFQyIsIngiOiJfLXkyLXd2MEVlOVJpaV84T0tqMXFfZ1E1ZEpTOFJSaDc5dGdCUkJMVFlJIiwieSI6ImVHZmNyNm1EV0VDbDNsMDhYS19WUDRnZ0I4TXJJbmpISVhLY1g3bkkwNTAiLCJjcnYiOiJQLTI1NiJ9fQ.EinqEBKfkX5hryFScL5k4UaP9YW5egKbImOb5aejNNY53t05-FTGUw.kBGxczCrUvkV5y0F.JA67aTm4Cyf4Ig.ZL96UugjU-4W_OZHocX22w";
			var privateEncryption = new Encryption(this.privateECKeyResolver.Object, this.logger.Object);

			// Act
			string actualValue = await privateEncryption.DecryptAsync(cipherData);

			// Assert
			Assert.AreEqual(expectedValue, actualValue);
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Decryption key not found. ID: '11'.")]
		public async Task DecryptAsync_PrivateKeyKidNotFound_Throws()
		{
			// Arrange								// Expected Kid == "11" 
			var cipherData = "{cipher}eyJhbGciOiJSU0EtT0FFUC0yNTYiLCJlbmMiOiJBMjU2R0NNIiwia2lkIjoiMTEiLCJjdHkiOiJ0ZXh0L3BsYWluIn0.AQk7JJN-LkHL21PaBf-y5yjlx3saquD_iZBhp-rz3zuageAvkKF0xEduWV3b5xUbYy9BqxZ4kzz95nYfmeoU4ySc6w1T3VrR9FGAO19vo0zM_O9ggYZKmoOQUTKV0dwLIQE1DF6Q0Wz5hqK3ygAtR_XFdcHy4xafo13GHInPBLWBWop78rdIwzjIeDiUeaToUFqAYvw7xWLmfWfcaft5T3urn0-6xI_1OECfQyzO0VtNB4ZB-u8KgZkLW5DiopdQGu_kNbHVx3hdysVWx2Nkz5OZqQGrfmbr_U6jhDZLAdKqhxJIS3BFuVWUAABs-RDKCoyNFfl4e75dL0kKBbPQJg.wY85Mqk5FQ95c7zb.nqP08KYxxGfpQg.6MVw7O6GJy6X0ZXozQFl3A";
			var privateEncryption = new Encryption(this.privateECKeyResolver.Object, this.logger.Object);
			// Actual Kid == 1

			// Act
			string actualValue = await privateEncryption.DecryptAsync(cipherData);

			// Assert
			// EncryptionException: "Decryption key not found. ID: '11'."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Unable to decrypt data.")]

		public async Task EncryptAsync_InvalidDecryptionData_Throws()
		{
			// Arrange
			string expectedValue = "Test data!";
			var publicEncryption = new Encryption(this.publicRSAKeyResolver.Object, this.logger.Object);
			var privateEncryption = new Encryption(this.privateRSAKeyResolver.Object, this.logger.Object);

			string cipherData = await publicEncryption.EncryptAsync(expectedValue);
			int idx = cipherData.LastIndexOf('.');
			cipherData = cipherData.Substring(0, idx + 1);
			cipherData += "badsig";  // Add invalid cipher

			// Act
			await privateEncryption.DecryptAsync(cipherData);

			// Assert
			// EncryptionException: "Unable to decrypt content or authentication tag do not match."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Unable to encrypt data.")]
		public async Task EncryptAsync_InvalidEncryptionData_Throws()
		{
			// Arrange
			string expectedValue = "Test data!";
			var jwksJson = File.ReadAllText(@"TestData\JwksRSAPublic.json");
			var jwks = JsonConvert.DeserializeObject<Jwks>(jwksJson);
			var rsaPublicJsonWebKey = jwks.Keys[0];
			rsaPublicJsonWebKey.N = "unknown";     // Use invalid key
			var publicRSAKeyResolver = new Mock<IKeyResolver>();
			publicRSAKeyResolver.Setup(k => k.GetEncryptionKeyAsync())
											 .ReturnsAsync(() => rsaPublicJsonWebKey);
			var publicEncryption = new Encryption(publicRSAKeyResolver.Object, this.logger.Object);

			// Act
			string cipherData = await publicEncryption.EncryptAsync(expectedValue);

			// Assert
			// EncryptionException: "Unable to encrypt data."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Unsupport Json Web Key type.")]
		public async Task EncryptAsync_PublicKeyNotFound_Throws()
		{
			// Arrange
			string expectedValue = "Test data!";           // Use OCT Key, not supported
			var publicEncryption = new Encryption(this.publicOctKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.EncryptAsync(expectedValue);

			// Assert
			// EncryptionException: "Unsupport Json Web Key type."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(SerializerException), "Unable to deserializer value.")]
		public async Task DecryptAsync_SerializerException_ArgumentNullException_Throws()
		{
			// Arrange
			string cipherData = "{cipher}";      // Missing cipher text
			var publicEncryption = new Encryption(this.publicRSAKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.DecryptAsync(cipherData);

			// Assert
			// SerializerException: "Unable to deserializer value."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(SerializerException), "Unable to deserializer value.")]
		public async Task DecryptAsync_SerializerException_ArgumentException_Throws()
		{
			// Arrange
			string cipherData = "{cipher}123456789123456789";      // Incorrect cipher text
			var publicEncryption = new Encryption(this.publicRSAKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.DecryptAsync(cipherData);

			// Assert
			// SerializerException: "Unable to deserializer value."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Decryption key not found. ID: '1'.")]

		public async Task DecryptAsync_PublicKeyNotSupported_Throws()
		{
			// Arrange
			var cipherJson = "{\"kty\": \"OCT\",\"k\": \"1234567890\",\"kid\": \"1\"}";
			var cipher = Constants.CIPHER_HEADER + Jose.Base64Url.Encode(Encoding.UTF8.GetBytes(cipherJson));
			var publicEncryption = new Encryption(this.publicOctKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.DecryptAsync(cipher);

			// Assert
			// EncryptionException: "Decryption key not found. ID: '1'."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Encryption key not found by KeyResolver.")]
		public async Task EncryptAsync_NoPublicKeysFound_Throws()
		{
			// Arrange
			string expectedValue = "Test data!";
			var publicEncryption = new Encryption(this.publicNullKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.EncryptAsync(expectedValue);

			// Assert
			// EncryptionException: "Encryption key not found by KeyResolver"
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		public async Task EncryptAsync_EncryptedValueContainsCipherPrefix()
		{
			// Arrange
			string expectedValue = "Test data!";
			var publicEncryption = new Encryption(this.publicRSAKeyResolver.Object, this.logger.Object);

			// Act
			string cipherData = await publicEncryption.EncryptAsync(expectedValue);

			// Assert
			Assert.IsTrue(cipherData.StartsWith("{cipher}", StringComparison.Ordinal));
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(InvalidHeaderException), "Invalid encryption header.")]
		public async Task DecryptAsync_InvalidEncryptionHeader_Throws()
		{
			// Arrange
			string cipherData = "SomeBadData";
			var publicEncryption = new Encryption(this.publicRSAKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.DecryptAsync(cipherData);

			// Assert
			// EncryptionException: "Invalid encryption header."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Encryption key not found by KeyResolver.")]
		public async Task EncryptAsync_NoKeysFound_Throws()
		{
			// Arrange
			string expectedValue = "Test data!";
			var publicEncryption = new Encryption(this.publicNullKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.EncryptAsync(expectedValue);

			// Assert
			// EncryptionException: "Encryption key not found by KeyResolver."
		}

		[TestMethod]
		[TestCategory("UnitTest")]
		[ExpectedExceptionMessage(typeof(EncryptionException), "Unsupport Json Web Key type.")]
		public async Task DecryptAsync_UnsupportJsonWebKeyType_Throws()
		{
			// Arrange					// OCT JsonWebKey type not supported
			var cipherJson = "{\"kty\": \"OCT\",\"k\": \"1234567890\",\"kid\": \"40F36B31-44A5-44B4-AC2F-676D5C8CB4C2\"}";
			var cipher = Constants.CIPHER_HEADER + Jose.Base64Url.Encode(Encoding.UTF8.GetBytes(cipherJson));
			var publicEncryption = new Encryption(this.privateOctKeyResolver.Object, this.logger.Object);

			// Act
			await publicEncryption.DecryptAsync(cipher);

			// Assert
			// EncryptionException: "Unsupport Json Web Key type."
		}
	}
}