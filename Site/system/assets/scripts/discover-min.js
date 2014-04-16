/*
 * Copyright (c) 2013 SDL Tridion Development Lab B.V. All rights reserved.
 */
(function(Context,undefined){var defaultProps={dpr:"((window.devicePixelRatio) ? window.devicePixelRatio : 1)",dw:"screen.width",dh:"screen.height",bcd:"screen.colorDepth",bw:"window.innerWidth",bh:"window.innerHeight",version:"1"};
var defaultCookiePath="/"+window.location.pathname.substr(1).split("/")[0];
Context.discover=function(props){discoverCapabilities(props);
Context.onResize(function(){discoverCapabilities(props)
},null)()
};
function discoverCapabilities(props){var _props={};
if(arguments.length===1){_props=mergeProperties(defaultProps,props)
}else{_props=defaultProps
}var cookie="";
for(var prop in _props){if(_props.hasOwnProperty(prop)){var val=eval(_props[prop]);
if(typeof val!="undefined"){cookie=cookie+prop+"~"+val+"|"
}}}setContextCookie(cookie)
}function mergeProperties(obj1,obj2){var obj3={};
var prop;
for(prop in obj1){if(obj1.hasOwnProperty(prop)){obj3[prop]=obj1[prop]
}}for(prop in obj2){if(obj2.hasOwnProperty(prop)){obj3[prop]=obj2[prop]
}}return obj3
}Context.getContextCookie=function(){var i=document.cookie.indexOf("context=");
if(i!=-1){var j=document.cookie.indexOf(";",i);
if(j==-1){j=document.cookie.length
}return document.cookie.substring(i+8,j)
}return""
};
function setContextCookie(contextCookie){document.cookie="context="+contextCookie+";path="+defaultCookiePath
}Context.clearContextCookie=function(){var cookieDate=new Date();
cookieDate.setTime(cookieDate.getTime()-1);
document.cookie="context=;path="+defaultCookiePath+";expires="+cookieDate.toUTCString()
};
Context.getVersionFromCookie=function(contextCookie){var contextAttr=contextCookie.split("|");
if(contextAttr.length>=3){var contextDetectedAttr=contextAttr[2].split("~");
if(contextDetectedAttr[0]=="version"){var foundVersion=contextDetectedAttr[1];
if(foundVersion==1){return true
}}}return false
};
Context.onResize=function(c,t){window.onresize=function(){clearTimeout(t);
t=setTimeout(c,500)
};
return c
};
function getAttributeFromCookie(contextCookie,attributeName){var i=contextCookie.indexOf(attributeName+"~");
var value="";
if(i!=-1){var j=contextCookie.indexOf("|",i);
if(j==-1){j=contextCookie.length
}value=contextCookie.substring(i+attributeName.length+1,j)
}return value
}}(window.Context=window.Context||{},null));
Context.discover(null);