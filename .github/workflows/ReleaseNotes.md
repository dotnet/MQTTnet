* [Core] Optimized packet serialization of PUBACK and PUBREC packets for protocol version 5.0.0 (#1939, thanks to @Y-Sindo).
* [Core] The package inspector is now fully async (#1941).
* [Client] Added a dedicated exception when the client is not connected (#1954, thanks to @marcpiulachs).
* [Server] The server will no longer send _NoMatchingSubscribers_ when the actual subscription was non success (#1965, BREAKING CHANGE!). 
* [ManagedClient] Added a new event (SubscriptionsChangedAsync) which is fired when a subscription or unsubscription was made (#1894, thanks to @pdufrene).
* [TopicTemplate] Added new extension which provides a template engine for topics (#1932, thanks to @simonthum).
