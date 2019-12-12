const AWS = require('aws-sdk');
AWS.config.update({region: 'us-east-1'});
const sql = require('mssql');
 
let response;

var errHandler = function(err) {
  console.log(err);
}

/**
 *
 * Event doc: https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-lambda-proxy-integrations.html#api-gateway-simple-proxy-for-lambda-input-format
 * @param {Object} event - API Gateway Lambda Proxy Input Format
 *
 * Context doc: https://docs.aws.amazon.com/lambda/latest/dg/nodejs-prog-model-context.html 
 * @param {Object} context
 *
 * Return doc: https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-lambda-proxy-integrations.html
 * @returns {Object} object - API Gateway Lambda Proxy Output Format
 * 
 */
exports.lambdaHandler = async (event, context) => {
    var sqlDB="mssql://FinalSystem:gnCeUWSzqw8eFMkgUBGrr2ES4qzGQs@172.30.20.12?database=Final&encrypt=true"; 
    try {
        // make sure that any items are correctly URL encoded in the connection string

        var bucketName = 'reporting-a2afrauddetect';
        var keyName = 'dude';
        var AWSS3 = new AWS.S3({apiVersion: '2006-03-01'})
        console.log("### QUERY" + AWSS3);
        AWSS3.listBuckets(function(err, data) {
          if (err) {
            console.log("### Error", err);
          } else {
            console.log("###  Success", data.Buckets);
          }
       });
      // console.log("### QUERY DONE");
    //   var bucketPromise = AWSS3.createBucket({Bucket: bucketName}).promise();
    //     // Call S3 to list the buckets
    //     bucketPromise.then(
    //       function(data) {
    //         console.log("made bucket");
    //       }).catch(
    //       function(err) {
    //         console.error("s3 bucket error:",err, err.stack);
    //         return err;
    //     });
  


            await sql.connect(sqlDB)    
            const result = await sql.query("SELECT p.ShardKey, u.UserName as 'Username', c.FullName as 'Customer Name', al.CreateOn as 'Create On', p.Id, p.ConfirmationNumber, p.Amount, p.PayFromAccount, al.VerifiedThrough as 'Pay From Account Verification', p.PayFromRtn, p.PayToBankAccount, p.PayToBankRoutingNumber FROM Payments p FULL OUTER JOIN Users u ON u.Id = p.UserId FULL OUTER JOIN Customers c ON c.Id = p.Customer_Id FULL OUTER JOIN A2AAuditLog al ON al.PaymentTemplateId = p.PaymentTemplateId WHERE p.AchPattern = 'A2A' and p.Amount > 250 and GETDATE() <= DATEADD(DAY, 30, al.CreateOn)")
            var json=JSON.stringify(result.recordset);


            var now = new Date();
            var logfile_name = 'fraud'; // + now.getFullYear() + "-"+ now.getMonth() + "-" + now.getDate() +'.json'
            console.log("LOG: "+logfile_name);
            var objectParams = {Bucket: bucketName, Key: logfile_name, Body: json};
            console.log("LOG: "+JSON.stringify(objectParams));
            
            var objectParams2 = {Bucket: bucketName, Key: 'jack', Body: 'dave'};
            var uploadPromise2 = AWSS3.putObject(objectParams2).promise();
            uploadPromise2.then(
              function(data) {
                console.log("Successfully uploaded data to " + bucketName + "/" + keyName);
              }).catch(
              function(err) {
                console.error(err, err.stack);
                return err;
              });

              
          var uploadPromise = AWSS3.putObject(objectParams).promise();
          uploadPromise.then(
            function(data) {
              console.log("Successfully uploaded data to " + bucketName + "/" + logfile_name);
            }).catch(
            function(err) {
              console.error(err, err.stack);
              return err;
            }).then(function(data) {
              console.log(data);
          }, errHandler);
            console.log("LOG: "+logfile_name);
            response = {
              'statusCode': 200,
              'body': json
            }

  

            //console.log(response.body);
      } catch (err) {
       console.log("s3 bucket error:",err);
       return err;
     }    
    return response
};
