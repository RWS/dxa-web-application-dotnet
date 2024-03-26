$(function () {
    const config = getConfig()
    const headers = { 
        "Content-Type": "application/json",
        "accept" : "application/json",                  
    }
    const pageLoader = $(".page-loader");
    const loader = $(".loader");
    // Post Services
    const postService = async (url, data) => {
        const ACCESS_TOKEN = await getAccessToken();
        return await $.ajax({
            type:"post",
            url : config.openapi_baseurl+url,
            data:JSON.stringify(data),
            headers:headers,
            beforeSend: function(xhr){
                withCredentials = true,
                xhr.setRequestHeader('Authorization', `Bearer ${ACCESS_TOKEN}`)
            },
            success:function(response){
                return response
            },
            error:function(error){
                console.log(error)
            }
        })
    }

    const getAccessToken = async () => {
        const cookies = document.cookie;
        if(cookies!==""){
            if(document.cookie.includes("access_token")){
                return ACCESS_TOKEN= getCookie("access_token");
            }
            else if(document.cookie.includes("refresh_token")){
               const refresh_token = getCookie("refresh_token");
                const response = await getTokenFrmRefreshToken(refresh_token);
                return response.access_token;
            }
        }
    }
    const getTokenFrmRefreshToken = async (refresh_token) =>{
        const clientId = getConfig();
        const redirectUri = window.location.origin
        const data = {
            refresh_token:refresh_token,
            grant_type:"refresh_token",
            client_id:clientId,
            redirect_uri:redirectUri
        }
        const headers = { 
            "Content-Type":  "application/x-www-form-urlencoded" ,
            "accept" : "application/json",                 
        }
        return await $.ajax({
            type:"post",
            url : `${config.authorization_baseurl}/token`,
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
                return response.access_token;
               // window.location.reload();                             
            },
            error:function(error){
                console.log(error)
            }
        })
    }
    // Put Services
    const putService = async (url, data) => {
        const ACCESS_TOKEN = await getAccessToken();
        return await $.ajax({
            type:"put",
            url : config.openapi_baseurl+url,
            data:JSON.stringify(data),
            headers:headers,
            beforeSend: function(xhr){
                withCredentials = true,
                xhr.setRequestHeader('Authorization', `Bearer ${ACCESS_TOKEN}`)
            },
            success:function(response){
                return response
            },
            error:function(error){
                console.log(error)
                loader.css("display", "none");
                pageLoader.css("display", "none");
            }
        })
    }
    // Publish status
    const getRequest = async (url) => {
        const ACCESS_TOKEN = await getAccessToken();
        return await $.ajax({
            type:"get",
            url : config.openapi_baseurl+url,
            headers:headers,
            beforeSend: function(xhr){
                withCredentials = true,
                xhr.setRequestHeader('Authorization', `Bearer ${ACCESS_TOKEN}`)
            },
            success:function(response){
                return response
            },
            error:function(error){
                console.log(error)
            }
        })
    }
    // Save component
    $(document).on("click", ".saveComponent", function (e) {
        e.stopPropagation();
        const that = $(this)
        const tcmid = e.currentTarget.closest("[data-component-id]").getAttribute("data-component-id").split(":").join('_');
        const indexPosition = e.currentTarget.closest("[data-index]").getAttribute("data-index")
        updateComponent(tcmid, that, indexPosition)
    })

    const getTargetPublicationId = async(componentId) => {
        const url = `/items/${componentId}?useDynamicVersion=true`
        const response = await getRequest(url);
        if(response.BluePrintInfo.IsShared){
            return response.BluePrintInfo.PrimaryBluePrintParentItem.IdRef;
        } else {
            return componentId;
        }
    }
    const updateComponent = async (tcmid, that, indexPosition) => {
            const inputField = that.closest("[data-fieldname]")
            let tagName = "";
            let inputValue = ""
            if(inputField.find("input[type=text]").attr("name")!==undefined){
                tagName = inputField.find("input[type=text]").attr("name");
                inputValue = inputField.find("input[type=text]").val();
            } else if(inputField.find('textarea').attr("name")!==undefined){
                tagName = inputField.find('textarea').attr("name");
                inputValue = tinymce.activeEditor.getContent("textarea");
            }
            
            //console.log(tagName, inputValue)
            loader.css("display", "block");
            pageLoader.css("display", "block");
            //Component Checkout
            const targetPublicationId =  await getTargetPublicationId(tcmid)
            const id = targetPublicationId.split(":").join("_")
            const checkoutUrl = `/items/${id}/checkOut`;
            const checkoutPromise = postService(checkoutUrl,{});
            
           // const inputValue = inputField.val();
            checkoutPromise.then(checkoutResponse => {
                if(checkoutResponse){
                    switch (tagName) {
                        case "subheading":
                            if(checkoutResponse.Content.hasOwnProperty("articleBody")){
                                checkoutResponse.Content.articleBody[indexPosition].subheading = inputValue;
                            }
                            if(checkoutResponse.Content.hasOwnProperty("itemListElement")){
                                checkoutResponse.Content.itemListElement[indexPosition].subheading = inputValue;
                            }
                            if(checkoutResponse.Content.hasOwnProperty("body")){
                                checkoutResponse.Content.body[indexPosition].subheading = inputValue;
                            }
                            break;
                        case "content" :
                            if(checkoutResponse.Content.hasOwnProperty("articleBody")){
                                checkoutResponse.Content.articleBody[indexPosition].content = inputValue;
                            }
                            if(checkoutResponse.Content.hasOwnProperty("itemListElement")){
                                checkoutResponse.Content.itemListElement[indexPosition].content.html = inputValue;
                            }
                            if(checkoutResponse.Content.hasOwnProperty("body")){
                                checkoutResponse.Content.body[indexPosition].content.html = inputValue;
                            }
                            break;
                        case "headline" :
                            checkoutResponse.Content.headline = inputValue;
                            //console.log(checkoutResponse)
                            break;
                        case "introduction" : 
                            checkoutResponse.Content.introduction = inputValue;
                            break;
                        case "linkText" : 
                            break;
                        case "caption" : 
                        if(checkoutResponse.Content.hasOwnProperty("articleBody")){
                            checkoutResponse.Content.articleBody[indexPosition].caption = inputValue;
                        }
                        if(checkoutResponse.Content.hasOwnProperty("itemListElement")){
                            checkoutResponse.Content.itemListElement[indexPosition].caption = inputValue;
                        }
                        if(checkoutResponse.Content.hasOwnProperty("body")){
                            checkoutResponse.Content.body[indexPosition].caption = inputValue;
                        }
                            break;
                        default:
                            break;
                    }
                    const updateUrl = `/items/${id}`;
                    //Component Update
                    const updatePromise = putService(updateUrl,checkoutResponse);
                    updatePromise.then(updateResponse => {
                        if(updateResponse){
                            const componentid = updateResponse.Id.replace(":","_");
                            //Component Checkin
                            const checkinUrl = `/items/${componentid}/checkIn`;
                            const checkInData = {"RemovePermanentLock": true}
                            const checkInPromise = postService(checkinUrl, checkInData);
                            checkInPromise.then(checkInResponse => {
                                if(checkInResponse){
                                    //console.log(checkInResponse);
                                    loader.css("display", "none");
                                    pageLoader.css("display", "none");
                                }
                            })
                            
                        }
                    })
                }
            })
    }
    
     // adding input field
    const loginStatus = () => {
        //const loginBtn = $(".loginStatus");
        //loginBtn.text("Login")       
        const inputFields = document.querySelectorAll("[data-fieldname]");
        //console.log(inputFields)
        disableEditor(inputFields, "disabledEditor")
        const cookies = document.cookie;
        //$("#tridion-bar").css("display", "none")
        if(cookies!==""){
            if(document.cookie.includes("access_token")) { 
                const access_token = getCookie("access_token")
                if(access_token!==""){
                    //$("#tridion-bar").css("display", "flex")
                    //loginBtn.text("Logout")
                    enableEditor(inputFields, "activeEditor")
                }
            } 
        } 
    } 
    const enableEditor = (inputFields) => {
        inputFields.forEach(items => {
            items.classList.remove("disabledEditor")
            items.classList.add("activeEditor")
        })
    }
    const disableEditor = (inputFields) =>{
        inputFields.forEach(items => {
            items.classList.add("disabledEditor")
            items.classList.remove("activeEditor")
        })
    }
    const getSchemaId = async(fieldName,value, componentId) => {
        const url =  `/items/${componentId}?useDynamicVersion=false`;
        const response = await getRequest(url);
        if(response){
            const schemaid =response.Schema.IdRef.split(":").join("_")
            getFieldType(fieldName,value, schemaid)
        }
    }
    const getFieldType = async(fieldName,value, schemid) => {
        const url =  `/items/${schemid}?useDynamicVersion=false`;
        const response = await getRequest(url);
        for (const key in response.Fields) {
            if(key===fieldName){
                if(response.Fields[fieldName].$type==="SingleLineTextFieldDefinition"){
                    inputFieldEditor(fieldName, value)
                }
            } else if(response.Fields[key].EmbeddedFields!==undefined  && response.Fields[key].EmbeddedFields[fieldName]!==undefined){
                if(response.Fields[key].EmbeddedFields[fieldName].$type==="XhtmlFieldDefinition"){
                    richTextEditor(fieldName, value)
                }
                if(response.Fields[key].EmbeddedFields[fieldName].$type==="SingleLineTextFieldDefinition"){
                    inputFieldEditor(fieldName, value)
                }
            }
        }
    }
    const editorHandlers = () => {
        return (
            `<div style="display: flex;align-items: flex-start;justify-content: flex-end; gap: 5px;line-height: 10px;">
                    <span style="cursor:pointer;" class="cancelComponentEditing">
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-x-square" viewBox="0 0 16 16">
                            <path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2z"/>
                            <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708"/>
                        </svg>
                    </span>
                    <span style="cursor:pointer;" class="saveComponent">
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-check-square" viewBox="0 0 16 16">
                            <path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2z"/>
                            <path d="M10.97 4.97a.75.75 0 0 1 1.071 1.05l-3.992 4.99a.75.75 0 0 1-1.08.02L4.324 8.384a.75.75 0 1 1 1.06-1.06l2.094 2.093 3.473-4.425a.235.235 0 0 1 .02-.022z"/>
                        </svg>
                    </span>
                </div>`
        )
    }
    const inputFieldEditor = (fieldName, value) => {
        if(value!==""){
            localStorage.setItem("clonedText", value.textContent)
            const inputField = `<input type="text" name="${fieldName}" value="${value.textContent}" id="xpm-edit" class="form-control" style="width:100%" />`+editorHandlers();
            value.innerHTML = inputField;
        } 
    }
    const richTextEditor = (fieldName, value) => {
        if(value!==""){
            localStorage.setItem("clonedText", value.textContent)
            const inputField = `<textarea type="text" name="${fieldName}" id="xpm-edit" class="form-control" style="width:100%; height:200px">${value.textContent}</textarea>`+editorHandlers();
                value.innerHTML = inputField;
        } 
        tinymce.remove();
        tinymce.init({
            selector: 'textarea',
            plugins: 'nonbreaking anchor autolink charmap codesample emoticons image link searchreplace table visualblocks wordcount linkchecker',
            toolbar: 'nonbreaking undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | link image table mergetags | addcomment showcomments | spellcheckdialog a11ycheck typography | align lineheight | indent outdent | emoticons charmap | removeformat',
            tinycomments_mode: 'embedded',
            tinycomments_author: 'Author name',
            entity_encoding:"raw",
            mergetags_list: [
            { value: 'First.Name', title: 'First Name' },
            { value: 'Email', title: 'Email' },
            ],
            ai_request: (request, respondWith) => respondWith.string(() => Promise.reject("See docs to implement AI Assistant")),
        });
    }

    $.fn.updateInputField = function (e) {
        const target = e.target;
        const region = target.closest("[data-region]").getAttribute("data-region")
        const targetName = target.name;
        const targetValue = target.value;
        const localStorageData = JSON.parse(localStorage.getItem(region))
        if (localStorageData !== null) {
            if (localStorageData[targetName] !== undefined) {
                localStorageData[targetName] = targetValue,
                    localStorage.setItem(region, JSON.stringify(localStorageData))
            } else {
                localStorageData[targetName] = targetValue
                localStorage.setItem(region, JSON.stringify(localStorageData))
            }
        } else {
            const componentRegion = {
                [targetName]: targetValue,
            }
            localStorage.setItem(region, JSON.stringify(componentRegion))
        }
    }

    $.fn.cancelEditing = function (e){
        const originalText  = localStorage.getItem("clonedText");
        e.target.closest("[data-fieldname]").innerHTML = originalText;
    }
    $(document).on("click", ".cancelComponentEditing", function(e){
        $(this).cancelEditing(e);
    })

    const elements = document.querySelectorAll('[data-fieldname]');
    elements.forEach(key => {
        
        key.addEventListener('dblclick', function(e){   
            if(e.currentTarget.classList[0]==="disabledEditor"){
                return
            }
            e.preventDefault();
            e.stopPropagation()
            $(this).showEditor(e.target);
        });
        key.addEventListener("change", function (e) {
            $(this).updateInputField(e);
        })
    });
	$.fn.showEditor = function (target) {
        const currentTarget = target;
        const value = currentTarget.closest("[data-fieldname]")
		const componentId =  currentTarget.closest("[data-component-id]").getAttribute("data-component-id").split(":").join("_");
		const name = currentTarget.closest("[data-fieldname]").getAttribute("data-fieldname");
		getSchemaId(name,value, componentId);  
    }
    loginStatus();
});