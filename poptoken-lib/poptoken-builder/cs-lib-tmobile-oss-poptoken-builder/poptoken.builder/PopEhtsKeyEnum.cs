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

using System.ComponentModel;

namespace com.tmobile.oss.security.taap.poptoken.builder
{
    public enum PopEhtsKeyEnum
    {
        [Description("Content-Type")]
        ContentType,

        [Description("application/json; charset=utf-8")]
        ApplicationJson,

        [Description("Cache-Control")]
        CacheControl,

        [Description("no-cache")]
        NoCache,

        [Description("Authorization")]
        Authorization,

        [Description("uri")]
        Uri,

        [Description("body")]
        Body,

        [Description("http-method")]
        HttpMethod,

        [Description("GET")]
        Get,

        [Description("POST")]
        Post,

        [Description("PUT")]
        Put,

        [Description("PATCH")]
        Patch,

        [Description("HEAD")]
        Head,

        [Description("OPTIONS")]
        Options,

        [Description("TRACE")]
        Trace,

        [Description("CONNECT")]
        Connect
    };
}
