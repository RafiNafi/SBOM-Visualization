using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


public class InputReader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        readFile();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void readFile()
    {
        string jsonTest = "{\r\n  \"SPDXID\": \"SPDXRef-DOCUMENT\",\r\n  \"spdxVersion\": \"SPDX-2.3\",\r\n  \"creationInfo\": {\r\n    \"created\": \"2023-08-30T04:40:16Z\",\r\n    \"creators\": [\r\n      \"Organization: Uchiha Cortez\",\r\n      \"Tool: FOSSA v0.12.0\"\r\n    ],\r\n    \"licenseListVersion\": \"3.18\",\r\n    \"documentDescribes\": [\r\n      \"SPDXRef-npm-codemirror-6.0.1\",\r\n      \"SPDXRef-npm-core-js-3.6.5\",\r\n      \"SPDXRef-npm-electron-11.1.1\",\r\n      \"SPDXRef-npm-fractional-1.0.0\",\r\n      \"SPDXRef-npm-regenerator-runtime-0.13.7\",\r\n      \"SPDXRef-npm-boolean-3.2.0\",\r\n      \"SPDXRef-npm-buffer-crc32-0.2.13\",\r\n      \"SPDXRef-npm-buffer-from-1.1.1\",\r\n      \"SPDXRef-npm-cacheable-request-6.1.0\",\r\n      \"SPDXRef-npm-clone-response-1.0.3\",\r\n      \"SPDXRef-npm-codemirror-autocomplete-6.9.0\",\r\n      \"SPDXRef-npm-codemirror-commands-6.2.5\",\r\n      \"SPDXRef-npm-codemirror-language-6.9.0\",\r\n      \"SPDXRef-npm-codemirror-lint-6.4.1\",\r\n      \"SPDXRef-npm-codemirror-search-6.5.2\",\r\n      \"SPDXRef-npm-codemirror-state-6.2.1\",\r\n      \"SPDXRef-npm-codemirror-view-6.17.0\",\r\n      \"SPDXRef-npm-concat-stream-1.6.2\",\r\n      \"SPDXRef-npm-config-chain-1.1.13\",\r\n      \"SPDXRef-npm-core-util-is-1.0.2\",\r\n      \"SPDXRef-npm-crelt-1.0.6\",\r\n      \"SPDXRef-npm-debug-2.6.9\",\r\n      \"SPDXRef-npm-debug-4.1.1\",\r\n      \"SPDXRef-npm-decompress-response-3.3.0\",\r\n      \"SPDXRef-npm-defer-to-connect-1.1.3\",\r\n      \"SPDXRef-npm-define-properties-1.1.3\",\r\n      \"SPDXRef-npm-detect-node-2.1.0\",\r\n      \"SPDXRef-npm-duplexer3-0.1.5\",\r\n      \"SPDXRef-npm-electron-get-1.14.1\",\r\n      \"SPDXRef-npm-encodeurl-1.0.2\",\r\n      \"SPDXRef-npm-end-of-stream-1.4.4\",\r\n      \"SPDXRef-npm-env-paths-2.2.1\",\r\n      \"SPDXRef-npm-es6-error-4.1.1\",\r\n      \"SPDXRef-npm-escape-string-regexp-4.0.0\",\r\n      \"SPDXRef-npm-extract-zip-1.7.0\",\r\n      \"SPDXRef-npm-fd-slicer-1.1.0\",\r\n      \"SPDXRef-npm-fs-extra-8.1.0\",\r\n      \"SPDXRef-npm-get-stream-4.1.0\",\r\n      \"SPDXRef-npm-get-stream-5.2.0\",\r\n      \"SPDXRef-npm-global-agent-3.0.0\",\r\n      \"SPDXRef-npm-globalthis-1.0.3\",\r\n      \"SPDXRef-npm-global-tunnel-ng-2.7.1\",\r\n      \"SPDXRef-npm-got-9.6.0\",\r\n      \"SPDXRef-npm-graceful-fs-4.2.4\",\r\n      \"SPDXRef-npm-http-cache-semantics-4.1.1\",\r\n      \"SPDXRef-npm-inherits-2.0.4\",\r\n      \"SPDXRef-npm-ini-1.3.8\",\r\n      \"SPDXRef-npm-isarray-1.0.0\",\r\n      \"SPDXRef-npm-json-buffer-3.0.0\",\r\n      \"SPDXRef-npm-jsonfile-4.0.0\",\r\n      \"SPDXRef-npm-json-stringify-safe-5.0.1\",\r\n      \"SPDXRef-npm-keyv-3.1.0\",\r\n      \"SPDXRef-npm-lezer-common-1.0.4\",\r\n      \"SPDXRef-npm-lezer-highlight-1.1.6\",\r\n      \"SPDXRef-npm-lezer-lr-1.3.10\",\r\n      \"SPDXRef-npm-lodash-4.17.20\",\r\n      \"SPDXRef-npm-lowercase-keys-1.0.1\",\r\n      \"SPDXRef-npm-lowercase-keys-2.0.0\",\r\n      \"SPDXRef-npm-lru-cache-6.0.0\",\r\n      \"SPDXRef-npm-matcher-3.0.0\",\r\n      \"SPDXRef-npm-mimic-response-1.0.1\",\r\n      \"SPDXRef-npm-minimist-1.2.5\",\r\n      \"SPDXRef-npm-mkdirp-0.5.5\",\r\n      \"SPDXRef-npm-ms-2.0.0\",\r\n      \"SPDXRef-npm-ms-2.1.2\",\r\n      \"SPDXRef-npm-normalize-url-4.5.1\",\r\n      \"SPDXRef-npm-npm-conf-1.1.3\",\r\n      \"SPDXRef-npm-object-keys-1.1.1\",\r\n      \"SPDXRef-npm-once-1.4.0\",\r\n      \"SPDXRef-npm-p-cancelable-1.1.0\",\r\n      \"SPDXRef-npm-pend-1.2.0\",\r\n      \"SPDXRef-npm-pify-3.0.0\",\r\n      \"SPDXRef-npm-prepend-http-2.0.0\",\r\n      \"SPDXRef-npm-process-nextick-args-2.0.1\",\r\n      \"SPDXRef-npm-progress-2.0.3\",\r\n      \"SPDXRef-npm-proto-list-1.2.4\",\r\n      \"SPDXRef-npm-pump-3.0.0\",\r\n      \"SPDXRef-npm-readable-stream-2.3.8\",\r\n      \"SPDXRef-npm-responselike-1.0.2\",\r\n      \"SPDXRef-npm-roarr-2.15.4\",\r\n      \"SPDXRef-npm-safe-buffer-5.1.2\",\r\n      \"SPDXRef-npm-semver-6.3.1\",\r\n      \"SPDXRef-npm-semver-7.5.4\",\r\n      \"SPDXRef-npm-semver-compare-1.0.0\",\r\n      \"SPDXRef-npm-serialize-error-7.0.1\",\r\n      \"SPDXRef-npm-sindresorhus-is-0.14.0\",\r\n      \"SPDXRef-npm-sprintf-js-1.1.2\",\r\n      \"SPDXRef-npm-string-decoder-1.1.1\",\r\n      \"SPDXRef-npm-style-mod-4.1.0\",\r\n      \"SPDXRef-npm-sumchecker-3.0.1\",\r\n      \"SPDXRef-npm-szmarczak-http-timer-1.1.2\",\r\n      \"SPDXRef-npm-to-readable-stream-1.0.0\",\r\n      \"SPDXRef-npm-tunnel-0.0.6\",\r\n      \"SPDXRef-npm-typedarray-0.0.6\",\r\n      \"SPDXRef-npm-type-fest-0.13.1\",\r\n      \"SPDXRef-npm-types-node-12.20.55\",\r\n      \"SPDXRef-npm-universalify-0.1.2\",\r\n      \"SPDXRef-npm-url-parse-lax-3.0.0\",\r\n      \"SPDXRef-npm-util-deprecate-1.0.2\",\r\n      \"SPDXRef-npm-w3c-keyname-2.2.8\",\r\n      \"SPDXRef-npm-wrappy-1.0.2\",\r\n      \"SPDXRef-npm-yallist-4.0.0\",\r\n      \"SPDXRef-npm-yauzl-2.10.0\"\r\n    ]\r\n  }}";

        dynamic jsonObj = JObject.Parse(jsonTest);

        var dict1 = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonTest);
        foreach (var kv in dict1)
        {
            Debug.Log(kv.Key + ":" + kv.Value);

            if(kv.Value.ToString().Contains("{") && kv.Value.ToString().Contains("}"))
            {
                var dict2 = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(kv.Value.ToString());

                foreach (var kv2 in dict2)
                {
                    Debug.Log(kv2.Key + ":" + kv2.Value);

                    if (kv2.Value.ToString().Contains("[") && kv2.Value.ToString().Contains("]"))
                    {
                        JArray arr = JArray.Parse(kv2.Value.ToString());

                        foreach (var kv3 in arr)
                        {
                            Debug.Log(kv3);
                        }
                    }
                }

            }
        }

    }

    public void recursiveRead()
    {

    }

}
