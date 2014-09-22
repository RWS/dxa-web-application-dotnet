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
        <rss version="2.0">
            <channel>
                <title>
                    <xsl:value-of select="Title"/>
                </title>
                <link>
                    <xsl:value-of select="$url"/>
                </link>
                <description>
                    <xsl:value-of select="Meta/description"/>
                </description>
                <language>
                    <xsl:variable name="lang" select="Meta/*[local-name() = 'item' and @item = 'og:locale']"/>
                    <xsl:value-of select="translate($lang, $uppercase, $lowercase)"/>
                </language>
                <xsl:for-each select="Regions/*/Items/item/Component">
                    <item>
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
                        <link>
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
                        </link>
                        <description>
                            <xsl:variable name="desc" select="MetadataFields/standardMeta/EmbeddedValues/item/description/Value"/>
                            <xsl:choose>
                                <xsl:when test="$desc != ''">
                                    <xsl:value-of select="$desc"/>
                                </xsl:when>
                                <xsl:otherwise>
                                    <xsl:value-of select="Fields/*[1]/Value"/>
                                </xsl:otherwise>
                            </xsl:choose>
                        </description>
                        <pubDate>
                            <xsl:value-of select="RevisionDate"/>
                        </pubDate>
                        <guid isPermaLink="false">
                            <xsl:value-of select="Id"/>
                        </guid>
                    </item>
                </xsl:for-each>
            </channel>
        </rss>
    </xsl:template>
</xsl:stylesheet>
