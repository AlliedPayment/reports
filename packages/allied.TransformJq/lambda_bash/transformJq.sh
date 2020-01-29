
# make a bucket.
# aws s3 mb s3://allied-deploy
# arn:aws:lambda:us-east-1:733408678024:layer:bash:1


# aws s3 cp example.json s3://allied-deploy
#./lambda_bash.sh -o update -s ex_script.sh

# aws s3 cp transformJqConfig.json s3://allied-deploy/.allied/transformJqConfig.json

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
    if [ $EVENT_DATA ]; then
        BUCKET=$(echo $EVENT_DATA | jq -r '.bucket')
        KEY=$(echo $EVENT_DATA | jq  -r '.key')
        
        #echo $BUCKET
        if [[ $BUCKET == "null" ]] ; then
            BUCKET=$(echo $EVENT_DATA | jq -r .Records[0].s3.bucket.name)
            #echo $BUCKET
        fi
        if [[ $KEY == "null" ]]; then
            KEY=$(echo $EVENT_DATA | jq -r .Records[0].s3.object.key)
        fi
    else
        echo "error: no event data to process;"
        exit 1
    fi
    
    set +e
    if [[ $BUCKET == "null" ]] || [[ $KEY = "null" ]] ; then
        echo "bucket or key not specified in s3 event. s3://$BUCKET/$KEY"
        exit 2
    else
        S3URL="s3://$BUCKET/$KEY"
        PROTECTED=$(echo $KEY | grep -c '.allied/*')
        if [[ $PROTECTED -gt 0 ]]; then
            echo "{\"success\": false, \"message\": \"error: will not process files in .allied/\"}" >&2
            exit 3
        fi
    fi
    echo $(bash --version)
    echo "processing URL: $S3URL"
    FLAG_DELETE=0
    FLAG_COPY=0
    FLAG_COPYLOCATION=""
    FLAG_ZIP=0

    KEY_NAME=${KEY%%.*}
    KEY_EXT=${KEY#*.}
    CONFIG_FILE=transformJqConfig.json
    aws s3 cp --quiet "s3://$BUCKET/.allied/$CONFIG_FILE" "$WORK_FOLDER/$CONFIG_FILE" || true
    if [ -f $WORK_FOLDER/$CONFIG_FILE ]; then
    #
        if [ $S3URL ]; then
            echo "aws s3 cp --quiet $S3URL $WORK_FOLDER/$KEY"
            aws s3 cp --quiet "$S3URL" "$WORK_FOLDER/$KEY"
            if [ -f "$WORK_FOLDER/$KEY" ]; then
                #echo "GIVEN FILE: "
                #echo $(cat "$WORK_FOLDER/$KEY")
                TRANSFORMS=$(jq -r '.transforms | keys | .[]' $WORK_FOLDER/$CONFIG_FILE)
                #printf "%s\n" ${TRANSFORMS[@]}
                for i in ${TRANSFORMS[@]}
                do : 
                    TRANSFORM_JSON=$(jq -r ".${i}" $WORK_FOLDER/$CONFIG_FILE)
                    if [ ${#TRANSFORM_JSON} -gt 0 ]; then
                        JQTRANSFORM=$(echo "$TRANSFORM_JSON" | jq -r ".jqTransform")            
                        JQOUTPUT=$(echo "$TRANSFORM_JSON" | jq -r ".jqOutputName")            
                        FILTER=$(echo "$TRANSFORM_JSON" | jq -r '.filterRegEx')
                        SHOULD_DEL=$(echo "$TRANSFORM_JSON" | jq -r '.deleteComplete')
                        COPY_COMPLETE=$(echo "$TRANSFORM_JSON" | jq -r '.copyComplete')
                        [ $COPY_COMPLETE == "null" ] && COPY_COMPLETE=""
                        ZIP_CONTENTS=$(echo "$TRANSFORM_JSON" | jq -r '.zipContents')
                        if [ ${#FILTER} -gt 0 ]; then
                            TRANSFORM_APPLIES=$(echo $KEY | grep -c $FILTER)
                            TRANSFORM_APPLIES=$(($TRANSFORM_APPLIES + 0))
                            if [ $TRANSFORM_APPLIES -gt 0 ]; then
                                printf 'KEY: %s FILTER: %s %s %d\n' $KEY $FILTER $i $TRANSFORM_APPLIES
                                echo "${i}($FILTER) is a match against $KEY; applying ${JQTRANSFORM} will delete: $SHOULD_DEL"

                                if [ ${#JQTRANSFORM} ]; then
                                    OUTPUTFILE="$WORK_FOLDER/output.txt"
                                    if [ ${#JQOUTPUT} ]; then
                                        JQOUTPUT=${JQOUTPUT/\[FILENAME\]/$KEY_NAME}
                                        OUTPUTFILE="$EXPORT_FOLDER/$JQOUTPUT"
                                    fi    
                                    echo "Transform($JQTRANSFORM) to $OUTPUTFILE";
                                    $(jq -r "${JQTRANSFORM}" $WORK_FOLDER/$KEY > $OUTPUTFILE)
                                fi

                                echo "COPYCOMPLETE: ${#COPY_COMPLETE} ${COPY_COMPLETE}" 
                                if [[ ${#COPY_COMPLETE} -gt 0 && "${COPY_COMPLETE}"!="null" ]]; then
                                    FLAG_COPYLOCATION=${COPY_COMPLETE}
                                    FLAG_COPY=1
                                    if [ ${#ZIP_CONTENTS}  -gt 0 ]; then
                                        FLAG_ZIP=1
                                    fi
                                fi
                                if [ ${SHOULD_DEL} -gt 0 ]; then
                                    FLAG_DELETE=1
                                fi
                            fi
                        else
                            echo "$i has no filter defined."
                        fi
                    else
                        echo "${i} has no transform defined."
                    fi
                done

                #aws s3 cp --quiet "s3://allied-deploy/$KEY_NAME.zip" "$WORK_FOLDER/$KEY_NAME.zip"        
                if [ ${FLAG_COPY} -gt 0 ]; then
                    if [ ${FLAG_ZIP}  -gt 0 ]; then
                        set +o noglob
                        echo  $(cd $EXPORT_FOLDER; zip $WORK_FOLDER/$KEY_NAME.zip $WORK_FOLDER/$KEY *)
                        set -o noglob
                        echo "COPY zip! aws s3 cp --quiet $WORK_FOLDER/$KEY_NAME.zip ${FLAG_COPYLOCATION}/$KEY_NAME.zip"
                        aws s3 cp --quiet "$WORK_FOLDER/$KEY_NAME.zip" "${FLAG_COPYLOCATION}/$KEY_NAME.zip"        
                    else
                        echo "COPY! aws s3 cp --quiet $WORK_FOLDER/$KEY ${FLAG_COPYLOCATION}/$KEY"
                        aws s3 cp --quiet "$WORK_FOLDER/$KEY" "${FLAG_COPYLOCATION}/$KEY"        
                    fi
                fi
                if [ ${FLAG_DELETE} -gt 0 ]; then
                    echo "DELETING($SHOULD_DEL)! $S3URL"
                    aws s3 rm  "$S3URL"
                    rm $WORK_FOLDER/$KEY
                fi
                echo "{'success': true}" >&2
                exit 0

            else 
                echo "$S3URL couldn't be transferred."
            fi
        else 
            echo "$CONFIG_FILE couldn't be transferred."
        fi
    else
        echo "No transforms defined. (define a .allied/$CONFIG_FILE)"
    fi
    echo "{'success': false}" >&2
}
