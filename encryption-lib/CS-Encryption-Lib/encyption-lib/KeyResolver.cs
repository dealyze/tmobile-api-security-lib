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

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace com.tmobile.oss.security.taap.jwe
{
	/// <summary>
	/// KeyResolver. Inject as Singleton
	/// </summary>
	public class KeyResolver : IKeyResolver, IDisposable
	{
		private static List<JsonWebKey> PublicJsonWebKeyList;
		private static List<JsonWebKey> PrivateJsonWebKeyList;
		private static bool IsCacheExpired;
		private static int JwksServiceCallCount;
		private static JwksService JwksService;

		private Timer timer;
		private bool isDisposed;

		/// <summary>
		/// Static Constructor
		/// </summary>
		static KeyResolver()
		{
			PublicJsonWebKeyList = new List<JsonWebKey>();
			PrivateJsonWebKeyList = new List<JsonWebKey>();
			IsCacheExpired = true;

			JwksService = default(JwksService);
			JwksServiceCallCount = 0;
		}

		/// <summary>
		/// Default Constructor
		/// </summary>
		private KeyResolver()
		{
			this.timer = new Timer();
			this.timer.Elapsed += OnTimedEvent;
			this.timer.AutoReset = false;
			this.timer.Enabled = false;
		}

		public KeyResolver(List<JsonWebKey> privateJsonWebKeyList, JwksService jwksService, long cacheDurationSeconds) : this()
		{
			PrivateJsonWebKeyList = privateJsonWebKeyList;
			JwksService = jwksService;

			this.timer.Interval = cacheDurationSeconds * 1000;
			IsCacheExpired = true;
		}

		/// <summary>
		/// Get Public JsonWebKey List
		/// </summary>
		/// <returns>JsonWebKey List</returns>
		public List<JsonWebKey> GetPublicJsonWebKeyList()
		{
			List<JsonWebKey> publicJsonWebKeyList = null;
			Interlocked.Exchange(ref publicJsonWebKeyList, PublicJsonWebKeyList);
			return publicJsonWebKeyList;
		}

		/// <summary>
		/// Set Public JsonWebKey List
		/// </summary>
		/// <param name="publicJsonWebKeyList">Public JsonWebKey List</param>
		public void SetPublicJsonWebKeyList(List<JsonWebKey> publicJsonWebKeyList)
		{
			Interlocked.Exchange(ref PublicJsonWebKeyList, publicJsonWebKeyList);
		}

		/// <summary>
		/// Get Private JsonWebKey List
		/// </summary>
		/// <returns>JsonWebKey List</returns>
		public List<JsonWebKey> GetPrivateJsonWebKeyList()
		{
			List<JsonWebKey> privateJsonWebKeyList = null;
			Interlocked.Exchange(ref privateJsonWebKeyList, PrivateJsonWebKeyList);
			return privateJsonWebKeyList;
		}

		/// <summary>
		/// Set Private JsonWebKey List
		/// </summary>
		/// <param name="privateJsonWebKeyList">Private JsonWebKey List</param>
		public void SetPrivateJsonWebKeyList(List<JsonWebKey> privateJsonWebKeyList)
		{
			Interlocked.Exchange(ref PrivateJsonWebKeyList, privateJsonWebKeyList);
		}

		/// <summary>
		/// Get Encryption Key Async
		/// </summary>
		/// <returns>Json Web Key</returns>
		public async Task<JsonWebKey> GetEncryptionKeyAsync()
		{
			var publicJsonWebKeyList = new List<JsonWebKey>();
			var jsonWebKey = default(JsonWebKey);

			if (IsCacheExpired)
			{
				// Only allow one thread at a time to call the JWKS service
				if (Interlocked.Increment(ref JwksServiceCallCount) == 1)
				{
					try
					{
						// Public RSA key
						publicJsonWebKeyList = await JwksService.GetJsonWebKeyListAsync();
						this.SetPublicJsonWebKeyList(publicJsonWebKeyList);

						IsCacheExpired = false;
						this.timer.Enabled = true;
					}
					finally
					{
						Interlocked.Exchange(ref JwksServiceCallCount, 0);
					}
				}
			}
			else
			{
				publicJsonWebKeyList = this.GetPublicJsonWebKeyList();
			}

			jsonWebKey = publicJsonWebKeyList.Find(k => k.Kty == "EC");
			if (jsonWebKey == null)
			{
				jsonWebKey = publicJsonWebKeyList.Find(k => k.Kty == "RSA");
				if (jsonWebKey == null)
				{
					throw new EncryptionException("Unable to retrieve public EC or RSA key from JWK store.");
				}
			}

			return jsonWebKey;
		}

		/// <summary>
		/// Get Decryption JsonWebKey Async
		/// </summary>
		/// <param name="kid">Key Id</param>
		/// <returns>JsonWebKey</returns>
		public async Task<JsonWebKey> GetDecryptionKeyAsync(string kid)
		{
			var privateJsonWebKey = this.GetPrivateJsonWebKeyList().Find(p => p.Kid == kid);
			return await Task.FromResult(privateJsonWebKey);
		}

		/// <summary>
		/// On Timed Event
		/// </summary>
		/// <param name="source">Timer</param>
		/// <param name="e">Elapsed Event Args</param>
		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			IsCacheExpired = true;
			this.timer.Enabled = false;
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Dispose the Timer
		/// </summary>
		/// <param name="disposing">Disposing</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.isDisposed)
			{
				if (disposing)
				{
					this.timer.Dispose();
				}

				this.isDisposed = true;
			}
		}
	}
}
