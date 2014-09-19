<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="no"/>
    <xsl:variable name="lowercase" select="'abcdefghijklmnopqrstuvwxyz'"/>
    <xsl:variable name="uppercase" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'"/>
    <xsl:template match="/root">
        <xsl:variable name="ref" select="Meta/*[local-name() = 'item' and @item = 'og:url']"/>
        <xsl:variable name="url">
            <xsl:choose>
                <xsl:when test="contains($ref, '?')">
                    <xsl:value-of select="substring-before($ref, '?')"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="$ref"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <feed xmlns="http://www.w3.org/2005/Atom">
            <id>
                <xsl:value-of select="$url"/>
            </id>
            <title>
                <xsl:value-of select="Title"/>
            </title>
            <updated>
                <xsl:value-of select="PageData/PageModified"/>
                <xsl:text>Z</xsl:text>
            </updated>
            <link rel="self">
                <xsl:attribute name="href">
                    <xsl:value-of select="Meta/*[local-name() = 'item' and @item = 'og:url']"/>
                </xsl:attribute>
            </link>
            <subtitle>
                <xsl:value-of select="Meta/description"/>
            </subtitle>
            <xsl:for-each select="Regions/Main/Items/item/Component">
                <xsl:variable name="link">
                    <xsl:choose>
                        <xsl:when test="contains($url, 'http://')">
                            <xsl:text>http://</xsl:text>
                            <xsl:value-of select="substring-before(substring-after($url, 'http://'), '/')"/>
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:text>https://</xsl:text>
                            <xsl:value-of select="substring-before(substring-after($url, 'https://'), '/')"/>
                        </xsl:otherwise>
                    </xsl:choose>
                    <xsl:text>/resolve/</xsl:text>
                    <xsl:value-of select="substring-after(Id, ':')"/>
                </xsl:variable>
                <entry>
                    <id>
                        <xsl:value-of select="$link"/>
                    </id>
                    <title>
                        <xsl:variable name="title" select="MetadataFields/standardMeta/EmbeddedValues/item/name/Value"/>
                        <xsl:choose>
                            <xsl:when test="$title != ''">
                                <xsl:value-of select="$title"/>
                            </xsl:when>
                            <xsl:otherwise>
                                <xsl:value-of select="Title"/>
                            </xsl:otherwise>
                        </xsl:choose>
                    </title>
                    <xsl:variable name="desc" select="MetadataFields/standardMeta/EmbeddedValues/item/description/Value"/>
                    <xsl:if test="$desc != ''">
                        <summary>
                            <xsl:value-of select="$desc"/>
                        </summary>
                    </xsl:if>
                    <updated>
                        <xsl:call-template name="convert-date">
                            <xsl:with-param name="date" select="RevisionDate"/>
                        </xsl:call-template>
                    </updated>
                    <link rel="alternate">
                        <xsl:attribute name="href">
                            <xsl:value-of select="$link"/>
                        </xsl:attribute>
                    </link>
                    <author>
                        <name>unknown</name>
                    </author>
                </entry>
            </xsl:for-each>
        </feed>
    </xsl:template>
    <xsl:template name="convert-date">
        <!-- Mon, 25 Aug 2014 11:59:42 GMT -->
        <!-- 2014-08-25T11:59:42Z -->
        <xsl:param name="date"/>
        <xsl:variable name="dayStart" select="substring-after($date, ', ')"/>
        <xsl:variable name="monthStart" select="substring-after($dayStart, ' ')"/>
        <xsl:variable name="yearStart" select="substring-after($monthStart, ' ')"/>
        <xsl:variable name="timeStart" select="substring-after($yearStart, ' ')"/>
        <xsl:variable name="day" select="substring-before($dayStart, ' ')"/>
        <xsl:variable name="month" select="substring-before($monthStart, ' ')"/>
        <xsl:variable name="year" select="substring-before($yearStart, ' ')"/>
        <xsl:variable name="time" select="substring-before($timeStart, ' ')"/>
        <xsl:value-of select="$year"/>
        <xsl:text>-</xsl:text>
        <xsl:choose>
            <xsl:when test="$month = 'Jan'">01</xsl:when>
            <xsl:when test="$month = 'Feb'">02</xsl:when>
            <xsl:when test="$month = 'Mar'">03</xsl:when>
            <xsl:when test="$month = 'Apr'">04</xsl:when>
            <xsl:when test="$month = 'May'">05</xsl:when>
            <xsl:when test="$month = 'Jun'">06</xsl:when>
            <xsl:when test="$month = 'Jul'">07</xsl:when>
            <xsl:when test="$month = 'Aug'">08</xsl:when>
            <xsl:when test="$month = 'Sep'">09</xsl:when>
            <xsl:when test="$month = 'Oct'">10</xsl:when>
            <xsl:when test="$month = 'Nov'">11</xsl:when>
            <xsl:when test="$month = 'Dec'">12</xsl:when>
        </xsl:choose>
        <xsl:text>-</xsl:text>
        <xsl:value-of select="$day"/>
        <xsl:text>T</xsl:text>
        <xsl:value-of select="$time"/>
        <xsl:text>Z</xsl:text>
    </xsl:template>
</xsl:stylesheet>
