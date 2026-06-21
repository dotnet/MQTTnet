* Client: Late acknowledgement packets (PUBACK, PUBCOMP, SUBACK, UNSUBACK) for canceled or timed-out requests no longer trigger a protocol violation and tear down the connection (#2078, #2079)
* Client: Add support for SOCKS5 proxies (#2251, thanks to @koepalex)
* Client: Fixed keep alive ping send timeout disconnect handling (#2253, thanks to @suhashollakc)
* Server: Fixed exposing Topic Alias to clients (#2250, thanks to @suhashollakc)
