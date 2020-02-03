

# make a bucket.
# aws s3 mb s3://allied-deploy
# arn:aws:lambda:us-east-1:733408678024:layer:bash:1
export LANG=C.UTF-8

read_jq()
{
    RET=$(echo "$1" | jq -e -r "$2" ) 
    retVal=$?    
    [ "$RET" == "null" ] && return 0
    if [ $retVal != 0 ]; then
        return 0;
    fi
    echo ${RET}
    return 1
}

echo_cmd()
{ 
    echo "cmd: $@"
    RET=$($@)
    retVal=$?
    echo "exit: $retVal $RET"   
}

exit_s3result()
{
    echo "{\"success\": $2, \"message\": \"$3\"}" >&2
    exit $1
}

handler ()
{
    #set -e
    shopt -s extglob
    set -o noglob
    EVENT_DATA=$1
    WORK_FOLDER=/tmp
    EXPORT_FOLDER=$WORK_FOLDER/export
    mkdir -p $EXPORT_FOLDER

    # bucket and key values come from s3 event or via bucket/key values in json provided.
    if [ ${#EVENT_DATA} -gt 0 ]; then
        BUCKET=$(read_jq "$EVENT_DATA" ".bucket")
        if [[ ${#BUCKET} -eq 0  ]] ; then
            BUCKET=$(read_jq "$EVENT_DATA" ".Records[0].s3.bucket.name")
        fi
        KEY=$(read_jq "$EVENT_DATA" ".key")
        if [[ ${#KEY} -eq 0 ]]; then
            KEY=$(read_jq "$EVENT_DATA" ".Records[0].s3.object.key")
        fi

        if [[ ${#BUCKET} -eq 0 ]] || [[ ${#KEY} -eq 0 ]] ; then
            exit_s3result 2 false "bucket or key not specified in s3 event. s3://$BUCKET/$KEY"
        fi
    else
        exit_s3result 1 false "error: no event data to process;"
    fi
    
    set +e
    S3URL="s3://$BUCKET/$KEY"
    echo "processing URL: $S3URL"
    PROTECTED=$(echo $KEY | grep -c '.allied/*')
    if [[ $PROTECTED -gt 0 ]]; then
        exit_s3result 3 false "error: will not process files in .allied/"
    fi
    KEY_NAME=${KEY%%.*}
    KEY_EXT=${KEY#*.}
    CONFIG_FILE=transformJqConfig.json
    aws s3 cp --quiet "s3://$BUCKET/.allied/$CONFIG_FILE" "$WORK_FOLDER/$CONFIG_FILE" || true
    [[ ! -f $WORK_FOLDER/$CONFIG_FILE ]] && exit_s3result 4 false "error: No transforms defined. (define a .allied/$CONFIG_FILE)"
    CONFIG=$(cat $WORK_FOLDER/$CONFIG_FILE)

    SHOULD_DEL=$(read_jq "$CONFIG" '.deleteFromSrcBucket // 0')
    COPY_TO_BUCKET=$(read_jq "$CONFIG" '.copyToBucket // 0')
    ZIP_OUTPUT=$(read_jq "$CONFIG" '.zipOutput // 0')
    
    echo "TOPLEVEL FLAGS: del: $SHOULD_DEL copy: $COPY_TO_BUCKET zip: $ZIP_OUTPUT"


    DEBUGLEVEL=$(read_jq "$CONFIG" '.debugLevel // 0')
    #[ ${DEBUGLEVEL} -gt 0 ] && set +x && echo "DEBUGLEVEL ${DEBUGLEVEL} +x"
    #[ ${DEBUGLEVEL} -gt 1 ] && set +u && echo "DEBUGLEVEL ${DEBUGLEVEL} +u"
    #[ ${DEBUGLEVEL} -gt 2 ] && set +v && echo "DEBUGLEVEL ${DEBUGLEVEL} +v"

    INCLUDE_ORGINAL=$(read_jq "$CONFIG" '.zipIncludeSrcFile')
    WORKING_FILE=$WORK_FOLDER/$KEY
    if [ ${INCLUDE_ORGINAL} ]; then
        echo "zip: including src file in $KEY_NAME.zip."
        WORKING_FILE=$EXPORT_FOLDER/$KEY
    fi


    aws s3 cp --quiet "$S3URL" "$WORKING_FILE"
    [[ ! -f $WORKING_FILE ]] && exit_s3result 5 false "error: $S3URL - $WORKING_FILE failed to transfer."
    #echo "GIVEN FILE: "
    #echo $(cat "$WORKING_FILE")
    TRANSFORMS=$(jq -e -r '.transforms | keys | .[]' $WORK_FOLDER/$CONFIG_FILE 2>&1)
    if [ $? -eq 4 ]; then
        exit_s3result 99 false "error: jq returned a parse error. ($TRANSFORMS)"
    fi
    #printf "%s\n" ${TRANSFORMS[@]}
    for t in ${TRANSFORMS[@]}
    do : 
        JSON=$(jq -r ".transforms | .[\"${t}\"]" $WORK_FOLDER/$CONFIG_FILE)
        if [ ${#JSON} -gt 0 ]; then
            JQTRANSFORM=$(read_jq "${JSON}" ".jqTransform")
            JQTRANSFORM_FILE=$(read_jq "${JSON}" ".jqTransformFile")
            JQOUTPUT=$(read_jq "${JSON}" ".jqOutputName")
            FILTER=$(read_jq "${JSON}" '.filterRegEx')
            if [ ${#FILTER} -gt 0 ]; then
                TRANSFORM_APPLIES=$(echo $KEY | grep -c $FILTER)
                if [[ $TRANSFORM_APPLIES -gt 0 && ${#JQTRANSFORM} -gt 0 ]]; then

                    OUTPUTFILE="$WORK_FOLDER/[FILENAME].${t}.txt"
                    if [ ${#JQOUTPUT} ]; then
                        OUTPUTFILE="$EXPORT_FOLDER/$JQOUTPUT"
                    fi    
                    OUTPUTFILE=${OUTPUTFILE/\[FILENAME\]/${KEY_NAME}}
                    echo "TRANSFORM: ${t}($FILTER) matches $KEY; transforming \"${JQTRANSFORM}\" --> ${OUTPUTFILE}"

                    TMPJQ="$WORK_FOLDER/tmp.jq"
                    if [ ${JQTRANSFORM_FILE} ]; then
                        aws s3 cp "s3://${BUCKET}/.allied/${JQTRANSFORM_FILE}" "$WORK_FOLDER/${JQTRANSFORM_FILE}"        
                        if [[ -f $WORK_FOLDER/${JQTRANSFORM_FILE} ]]; then
                            cat "$WORK_FOLDER/${JQTRANSFORM_FILE}" > $TMPJQ 
                            echo $JQTRANSFORM >> $TMPJQ
                        else
                            echo "error downloading /.allied/${JQTRANSFORM_FILE}"
                            echo "s3://${BUCKET}/.allied/${JQTRANSFORM_FILE} $WORK_FOLDER/${JQTRANSFORM_FILE}"
                            continue
                        fi
                    else
                        echo $JQTRANSFORM > "$TMPJQ"
                    fi
                    #echo "FILTER: " $(cat $TMPJQ)
                    CMDJQ="jq -r -f $TMPJQ ${WORKING_FILE}"
                    #echo "JQCMD: $CMDJQ"
                    RET=$($CMDJQ >${OUTPUTFILE}  2>&1)
                    #retVal=$?
                    #echo "exit: $retVal $RET"   
                    #echo "INPUT: " $(cat $WORKING_FILE)
                    #echo "CMD: $CMDJQ"
                    #echo_cmd $CMDJQ
                    #echo "RESULT: ${OUTPUTFILE} ||| " 
                    #echo $(cat ${OUTPUTFILE})
                fi
            else
                echo "${t} has no filter defined."
            fi
        else
            echo "${t} has no transform defined."
        fi
    done

    if [ ${#COPY_TO_BUCKET} -gt 0 ]; then
        if [ ${ZIP_OUTPUT} = true ]; then
	    ZIPFILE="$WORK_FOLDER/$KEY_NAME.zip"
            set +o noglob
            cd $EXPORT_FOLDER
            ZIPCMD="zip $ZIPFILE *"
            echo_cmd ${ZIPCMD}
            aws s3 cp ${ZIPFILE} "${COPY_TO_BUCKET}/$KEY_NAME.zip"        
            set -o noglob
        else
            COPYCMD="aws s3 cp --quiet \"$WORKING_FILE\" \"${COPY_TO_BUCKET}/$KEY\""
            echo_cmd ${COPYCMD}
        fi
    fi

    if [ ${SHOULD_DEL} -gt 0 ]; then
        echo "DELETING($SHOULD_DEL)! $S3URL"
        aws s3 rm  "$S3URL"
    fi
    echo "{'success': true}" >&2
    exit 0
}
