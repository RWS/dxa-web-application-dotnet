const siteUrl = window.location.origin;

function getCookie(cookieName){
    let cookie = {};
    document.cookie.split(";").forEach(item => {
        let[key, value] = item.split("=");
        cookie[key.trim()] = value;
    })
    return cookie[cookieName]
}

function logout(){
    $.cookie('access_token', "", -1);
} 

function login(){
    const loginText = document.querySelector(".loginStatus").textContent;
    if(loginText==="Logout"){
        logout();
        window.location.reload()
    } else{
        const config = getConfig()
        const client_id = config.client_id
        const redirect_uri =siteUrl;
        const scope = "openid profile role forwarded offline_access";
        const response_type = "code";
        const grant_type =  "authorization_code";
        const headers = { 
            "Content-Type":  "application/x-www-form-urlencoded" ,
            "accept" : "application/json",                 
        }
        const cookies = document.cookie;
        if(cookies!==""){
            if(!document.cookie.includes("access_token")) { 
                if(document.cookie.includes("refresh_token")){
                   const refresh_token = getCookie("refresh_token")
                   getTokenFrmRefreshToken(client_id, refresh_token, redirect_uri, headers)
                }
            }
        }
        Authorize({
            path: `${config.authorization_baseurl}/authorize?client_id=${client_id}&response_type=${response_type}&redirect_uri=${redirect_uri}&scope=${scope}`,
            callback: function(response){
                //console.log('callback', response);
                const urlParams = new URLSearchParams(response.location.search);
                const authorizationCode = urlParams.get('code');
                const data = {
                    code:authorizationCode,
                    grant_type:grant_type,
                    client_id:client_id,
                    redirect_uri:redirect_uri
                }
                $.ajax({
                    type:"post",
                    url :   `${config.authorization_baseurl}/token`,
                    data:data,
                    headers:headers,
                    success:function(response){
                        //console.log(response)
                        let currentTime = new Date();
                        const expiresIn = response.expires_in
                        const accessTokenExpiryTime = new Date(currentTime.getTime() + 1000 * 300);
                        const refreshTokenExpiryTime = new Date(currentTime.getTime() + 1000 * expiresIn);
                        document.cookie = `access_token = ${response.access_token}; expires = ${accessTokenExpiryTime}; path="/"`;
                        document.cookie = `refresh_token = ${response.refresh_token}; expires = ${refreshTokenExpiryTime}; path="/"`;
                        //return response;
                        window.location.reload();                             
                    },
                    error:function(error){
                        console.log(error)
                    }
                })
            }
        });
    }
}
function Authorize(options){
    options.windowName = options.windowName ||  'ConnectWithOAuth'; // should not include space for IE
    options.windowOptions = options.windowOptions || 'location=0,status=0,width=800,height=400';
    options.callback = options.callback || function(){ 
        window.location.reload(); 
    };
    var that = this;
   // console.log(options.path);
    that._oauthWindow = window.open(options.path, options.windowName, options.windowOptions);
    that._oauthInterval = window.setInterval(function(){
        if(that._oauthWindow!==null && that._oauthWindow.location.hasOwnProperty("href")){
            if (that._oauthWindow.location.href.includes(siteUrl)) {
                that._oauthWindow.close()
            }
        }
        if (!that._oauthWindow || !that._oauthWindow.closed) {
            return
        };
        clearInterval(that._oauthInterval);
        options.callback(that._oauthWindow);
    }, 1000);
} 
function getTokenFrmRefreshToken(client_id, refresh_token, redirect_uri, headers){
    const config = getConfig()
    const data = {
        refresh_token:refresh_token,
        grant_type:"refresh_token",
        client_id:client_id,
        redirect_uri:redirect_uri
    }
    $.ajax({
        type:"post",
        url : `${config.authorization_baseurl}/token`,
        data:data,
        headers:headers,
        success:function(response){
           // console.log(response)
            let currentTime = new Date();
            const expiresIn = response.expires_in
            const accessTokenExpiryTime = new Date(currentTime.getTime() + 1000 * 300);
            const refreshTokenExpiryTime = new Date(currentTime.getTime() + 1000 * expiresIn);
            document.cookie = `access_token = ${response.access_token}; expires = ${accessTokenExpiryTime}; path="/"`;
            document.cookie = `refresh_token = ${response.refresh_token}; expires = ${refreshTokenExpiryTime}; path="/"`;
           // return response;
            window.location.reload();                             
        },
        error:function(error){
            console.log(error)
        }
    })
}