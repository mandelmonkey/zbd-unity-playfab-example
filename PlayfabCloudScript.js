var MAX_VALUE = 10;
handlers.getUserData = function(args, context) {
    var titleData = server.GetTitleInternalData({});
    var headers = {
        "apikey": titleData.Data.ZEBEDEE_API_KEY
    };

    args.client_secret = titleData.Data.ZEBEDEE_LOGIN_CLIENT_SECRET;

    var url = "https://api.zebedee.io/v1/oauth2/token";
    var content = JSON.stringify(args);
    var httpMethod = "post";
    var contentType = "application/json";

    var response = http.request(url, httpMethod, content, contentType, headers);

    var responseObject = JSON.parse(response);
    var {
        access_token
    } = responseObject;

    headers = {
        "usertoken": access_token,
        "apikey": titleData.Data.ZEBEDEE_API_KEY
    };
    var url = "https://api.zebedee.io/v1/oauth2/user";
    var httpMethod = "get";
    var contentType = "application/json";

    // The pre-defined http object makes synchronous HTTP requests
    response = http.request(url, httpMethod, null, contentType, headers);
    log.info(response);

    responseObject = JSON.parse(response);
    const {
        success,
        data
    } = responseObject;

    if (!success) {
        return {
            type: "login",
            success: false,
            data: "something went wrong"
        };
    }
    const {
        gamertag,
        id,
        email,
        isVerified
    } = data;
    server.UpdateUserInternalData({
        PlayFabId: currentPlayerId, // automatically provided in CloudScript
        Data: {
            "gamerTag": gamertag,
            "zbdUserId": id,
            "email": email,
            "isZBDVerified": isVerified
        }
    });

    return {
        type: "login",
        success: true,
        data
    };

};

handlers.getPoints = function(args, context) {

    var userData = server.GetUserInternalData({
        PlayFabId: currentPlayerId,
        Keys: ["gamerTag"],
        IfChangedFromDataVersion: 0
    });
    log.info(userData);
    if (!userData.Data.gamerTag) {
        return {
            success: false,
            data: "please login with ZEBEDEE"
        };
    }

    var inventory = server.GetUserInventory({
        PlayFabId: currentPlayerId
    });

    var points = Number(inventory.VirtualCurrency["PT"]);

    return {
        type: "getPoints",
        succes: true,
        data: points
    };
};

handlers.addPoint = function(args, context) {

    var userData = server.GetUserInternalData({
        PlayFabId: currentPlayerId,
        Keys: ["gamerTag"],
        IfChangedFromDataVersion: 0
    });
    if (!userData.Data.gamerTag) {
        return {
            type: "addPoint",
            success: false,
            data: "please login with ZEBEDEE"
        };
    }

    var inventory = server.GetUserInventory({
        PlayFabId: currentPlayerId
    });

    var points = Number(inventory.VirtualCurrency["PT"]);

    if (points >= MAX_VALUE) {
        return {
            type: "addPoint",
            success: false,
            data: "max value reached"
        };
    }

    var addResult = addPoints(1);

    return {
        type: "addPoint",
        success: true,
        data: addResult.Balance
    };
};

function getGamerTagForZBDUserId(zbdUserId) {
    try {
        var titleData = server.GetTitleInternalData({});
        var headers = {
            "apikey": titleData.Data.ZEBEDEE_API_KEY
        };

        var url = "https://api.zebedee.io/v0/gamertag/user-id/" + zbdUserId;
        var httpMethod = "get";
        var contentType = "application/json";

        // The pre-defined http object makes synchronous HTTP requests
        const response = http.request(url, httpMethod, null, contentType, headers);

        const responseObject = JSON.parse(response);
        const {
            success,
            data
        } = responseObject;

        if (success) {
            const {
                gamertag
            } = data;

            return gamertag;
        } else {
            log.error("e1" + response);
            throw new Error(response)
        }
    } catch (e) {
        log.error("e2" + e);
        throw new Error(e)
    }

}


function sendToGamerTag(amount, gamertag) {
    try {
        var titleData = server.GetTitleInternalData({});
        var headers = {
            "apikey": titleData.Data.ZEBEDEE_API_KEY
        };

        var body = {
            amount,
            gamertag,
            description: "thanks for playing"
        }
        var url = "https://api.zebedee.io/v0/gamertag/send-payment";
        var content = JSON.stringify(body);
        var httpMethod = "post";
        var contentType = "application/json";

        var response = http.request(url, httpMethod, content, contentType, headers);

        const responseObject = JSON.parse(response);
        return responseObject;
    } catch (e) {
        log.error("e2" + e);
        throw new Error(e)
    }

}

function resetPoints(points) {
    var subtractResult = server.SubtractUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "PT",
        Amount: points
    });

    return subtractResult;
}


function addPoints(points) {
    var addResult = server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "PT",
        Amount: points
    });

    return addResult;
}

function updateUserSats(sats) {
    var userData = server.GetUserInternalData({
        PlayFabId: currentPlayerId,
        Keys: ["satsSent"],
        IfChangedFromDataVersion: 0
    });
    var userSatsSent = 0;

    if (userData.Data.satsSent) {
        userSatsSent = Number(userData.Data.satsSent.Value);
    }
    userSatsSent +=  Number(sats);

    server.UpdateUserInternalData({
        PlayFabId: currentPlayerId, // automatically provided in CloudScript
        Data: {
            "satsSent": userSatsSent,
        }
    });


}
handlers.withdraw = function(args, context) {
    try {

        var userData = server.GetUserInternalData({
            PlayFabId: currentPlayerId,
            Keys: ["zbdUserId","isZBDVerified"],
            IfChangedFromDataVersion: 0
        });
        
      
        if (!userData.Data.isZBDVerified.Boolean) {
            return {
                type: "withdraw",
                success: false,
                data: "please verify in the ZEBEDEE app to withdraw"
            };
        }

        if (!userData.Data.zbdUserId.Value) {
            return {
                type: "withdraw",
                success: false,
                data: "please login with ZEBEDEE"
            };
        }
        log.info(userData.Data.zbdUserId.Value);
        var inventory = server.GetUserInventory({
            PlayFabId: currentPlayerId
        });

        var points = Number(inventory.VirtualCurrency["PT"]);

        if (points == 0) {
            return {
                type: "withdraw",
                success: false,
                data: "you do not have any points"
            };
        }
        if (points >= MAX_VALUE) {
            points = MAX_VALUE;
        }


     resetPoints(points);


        var milliSatsToSend = points * 1000;

        const gamertag = getGamerTagForZBDUserId(userData.Data.zbdUserId.Value);

        var responseObject = sendToGamerTag(milliSatsToSend, gamertag);

        if (!responseObject.success) {

          addPoints(points);

            return {
                type: "withdraw",
                success: false,
                data: "error sending"
            };
        }

       
        updateUserSats(points);

        return {
            type: "withdraw",
            success: true,
            data: points + " sats sent!"
        };

    } catch (e) {
        log.error("e3 " + e);
        return {
            type: "withdraw",
            success: false,
            data: e + ""
        };
    }
};