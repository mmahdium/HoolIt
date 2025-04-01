# HoolIt

## An open-source dweet.io Alternative (RIP dweet.io)

**HoolIt** is an open-source project aiming to provide a simple and easy-to-use alternative to the beloved [dweet.io](http://dweet.io), which sadly is [no longer available](https://x.com/dweet_io/status/1899886062580703423).  If you were a fan of dweet.io's simplicity for quickly sending and receiving data from your "things," HoolIt is designed for you!
---

**✨ Try it instantly!**

For **quick testing and exploration**, you can use a **free, publicly hosted instance of HoolIt** at:

**[https://hoolit.mahdium.ir](https://hoolit.mahdium.ir)**
---
**It uses the exact same API scheme as dweet.io**, making migration and understanding incredibly straightforward. No signup, no setup, it just works (once you have HoolIt running, of course!).

---
### Feature Status

*   ✅ **Dweeting (Sending Data):** [![Dweeting](https://img.shields.io/badge/Dweeting-Implemented-brightgreen?style=flat-square)](https://shields.io)
*   ✅ **Real-time Streams (Listening):** [![Real-time Streams](https://img.shields.io/badge/Real--time%20Streams-Implemented-brightgreen?style=flat-square)](https://shields.io)
*   ✅ **dweet.io API Compatibility (Core):** [![API Compatibility](https://img.shields.io/badge/dweet.io%20API%20Compat-Implemented-brightgreen?style=flat-square)](https://shields.io)
*   ✅ **HTTP & HTTPS Support:** [![HTTP & HTTPS](https://img.shields.io/badge/HTTP%20%26%20HTTPS-Implemented-brightgreen?style=flat-square)](https://shields.io)
*   ✅ **Query Parameters for Dweets:** [![Query Parameters](https://img.shields.io/badge/Query%20Parameters-Implemented-brightgreen?style=flat-square)](https://shields.io)
*   ❌ **JSONP Support:**  [![JSONP Support](https://img.shields.io/badge/JSONP%20Support-Not%20Implemented-red?style=flat-square)](https://shields.io)
*   ❌ **Dweet History (Getting Dweets):** [![Dweet History](https://img.shields.io/badge/Dweet%20History-Not%20Implemented-red?style=flat-square)](https://shields.io)
---
---

## HoolIt API Documentation (dweet.io Compatible)

### Dweeting (Sending Data)

Send data from your thing to the cloud by "dweeting" it with a simple HAPI web API, just like you did with dweet.io.

**To dweet from your thing, simply call a URL like:**

```
https://hoolit.mahdium.ir/dweet/for/{your-thing-name}?your_key=your_value&another_key=another_value
```

**Replace:**

*   `{your-thing-name}` with a unique name for your "thing." This is how you will identify your data streams.
*   `your_key=your_value&another_key=another_value` with any query parameters you want to include in your dweet.

**Example:**

To send data for a thing named `my-sensor` with temperature and humidity values:

```
https://hoolit.mahdium.ir/dweet/for/my-sensor?temperature=25&humidity=60
```

**Any query parameters you add to the request will be added as key-value pairs to the content of the dweet.**

**Response:**

HoolIt will respond with a JSON object in the following format upon successful dweeting:

```json
{
  "this": "succeeded",
  "by": "dweeting",
  "the": "dweet",
  "with": {
    "thing": "{your-thing-name}",
    "created": "2024-XX-XXTXX:XX:XX.XXXZ", // Date and time of dweet creation in UTC
    "content": {
      "your_key": "your_value",
      "another_key": "another_value"
      // ... any other query parameters you sent
    }
  }
}
```

**Important Notes:**

*   **HTTPS and HTTP Support:**  While HTTPS is recommended for secure connections (`https://hoolit.mahdium.ir`), HoolIt also supports un-secure HTTP connections (`http://hoolit.mahdium.ir`) for devices that might not support SSL. Use HTTP with caution, especially for sensitive data.
*   **POST Requests and JSON Body:** Sending JSON data in the body of a POST request is **not currently supported**. Only query parameters in GET requests are processed for dweeting. This is a planned feature for future releases.

---

### Getting Dweets (Reading Data)

<p align="center">
  <img src="https://img.shields.io/badge/WARNING-NOT%20IMPLEMENTED%20YET-red?style=for-the-badge&logo=warning" alt="Not Implemented Yet">
</p>

**<p style="color:red; font-weight:bold;">IMPORTANT:  The functionality to retrieve past dweets (using <code>/get/latest/dweet/for/</code> or <code>/get/dweets/for/</code> endpoints from dweet.io) is currently <ins>NOT IMPLEMENTED</ins> in HoolIt.</p>**

**<p style="font-weight:bold;">HoolIt currently <ins>DOES NOT STORE ANY DWEET HISTORY</ins>.</p>**

In this version of HoolIt, there is no database or persistent storage for dweets.  Data is only transiently available for real-time streaming (see below).

**Future development may include options for storing dweet history, but for now, HoolIt is focused on real-time data streaming.**

---

### Real-time Streams (Listening for Data)

You can create a real-time subscription to dweets using Server-Sent Events (SSE). This allows you to receive dweets as they are sent to HoolIt for a specific "thing."

**To listen for real-time dweets, make a request to:**

```
https://hoolit.mahdium.ir/listen/for/dweets/from/{your-thing-name}
```

**Replace:**

*   `{your-thing-name}` with the "thing" name you want to subscribe to.

**Example:**

To listen for real-time dweets for the thing named `my-sensor`:

```
https://hoolit.mahdium.ir/listen/for/dweets/from/my-sensor
```

**(This endpoint is designed for server-side consumption and will not work directly in a standard web browser address bar due to the nature of Server-Sent Events.)**

**Using `curl` (or similar tools) to test:**

From a Unix command line or terminal, you can use `curl` to observe the real-time stream:

```bash
curl -N https://hoolit.mahdium.ir/listen/for/dweets/from/my-sensor
```

**`-N` option in `curl` is important to disable buffering and see the stream in real-time.**

**Data Format in the Stream:**

The server will keep the connection alive and send you dweets as they arrive in JSON format, like this:

```json
{"thing":"{your-thing-name}","created":"2024-XX-XXTXX:XX:XX.XXXZ","content":{"your_key":"your_value", "another_key":"another_value"}}
{"thing":"{your-thing-name}","created":"2024-XX-XXTXX:XX:XX.XXXZ","content":{"another_key":"some_other_value"}}
// ... and so on, as new dweets arrive for "{your-thing-name}"
```

Each line in the stream is a new dweet. You will need to parse each line as a JSON object.

---

**API Compatibility with dweet.io:**

**HoolIt is designed to be fully API compatible with dweet.io for the implemented features (dweeting and real-time streaming).** You should be able to replace `dweet.io` URLs with `hoolit.mahdium.ir` (or your own self hosted instance) in your existing projects and code that used dweet.io for sending and listening to data, and it should function without code changes (for the compatible API parts).


---

## Running HoolIt with Docker (Quick & Easy)

HoolIt is easily run with Docker!  You have two main options: `docker run` for a quick start, or Docker Compose for a slightly more structured setup.

**⚡️ Quick Start with `docker run`**

The fastest way to run HoolIt is with this command:

```bash
docker run -p 5030:5030 -d ghcr.io/mmahdium/hoolit:main
```

This command:

*   Pulls the HoolIt Docker image (`ghcr.io/mmahdium/hoolit:main`).
*   Runs it in the background (`-d`).
*   Makes HoolIt accessible on port `5030` of your computer (`-p 5030:5030`).

You can then access HoolIt at `http://localhost:5030`

**🐳  Running with Docker Compose (Slightly More Setup)**

For a slightly more structured approach, especially if you plan to configure things further, use Docker Compose.

1.  **Create `docker-compose.yaml`:**

    ```yaml
    version: '3.8'
    services:
      hoolit:
        image: ghcr.io/mmahdium/hoolit:main
        ports:
          - "5030:5030"
    ```

2.  **Run in the same directory as `docker-compose.yaml`:**

    ```bash
    docker-compose up -d
    ```

This does essentially the same as `docker run` but uses a configuration file. Stop it later with `docker-compose down`.

---

**However, for production, privacy, or customized setups, it is highly recommended to run your own instance using Docker as described above.**  The public instance is provided as a convenience and may have limitations or be reset periodically.

---


**Contributing:**

HoolIt is an open-source project, and contributions are welcome! If you want to contribute to development, add features, or report issues, please check out the GitHub repository: [https://github.com/mmahdium/HoolIt](https://github.com/mmahdium/HoolIt).

---

**License:**

[https://github.com/mmahdium/HoolIt?tab=AGPL-3.0-1-ov-file#readme](https://github.com/mmahdium/HoolIt?tab=AGPL-3.0-1-ov-file#readme)

---

**Disclaimer:**

HoolIt is an independent project and is not affiliated with or endorsed by the original dweet.io service. It is intended as a community-driven, open-source alternative.  Use it at your own risk.

---
